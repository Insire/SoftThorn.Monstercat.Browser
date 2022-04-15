using NAudio.Wave;
using SoftThorn.Monstercat.Browser.Core;
using SoftThorn.MonstercatNet;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    internal sealed class PlaybackService : IPlaybackService
    {
        private readonly DispatcherTimer _timer;
        private readonly IMonstercatApi _api;

        private volatile StreamingPlaybackState _playbackState;
        private volatile bool _fullyDownloaded;

        private VolumeWaveProvider16? _volumeProvider;
        private BufferedWaveProvider? _bufferedWaveProvider;
        private IWavePlayer? _waveOut;
        private CancellationTokenSource? _cancellationTokenSource;

        private bool IsBufferNearlyFull => _bufferedWaveProvider != null
            && _bufferedWaveProvider.BufferLength - _bufferedWaveProvider.BufferedBytes < _bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4;

        private static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
        {
            var waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2, frame.FrameLength, frame.BitRate);
            return new AcmMp3FrameDecompressor(waveFormat);
        }

        public PlaybackService(IMonstercatApi api, DispatcherTimer timer)
        {
            _api = api;
            _timer = timer;
            _timer.Tick += OnTimerTick;
        }

        public async Task Play(TrackStreamRequest request)
        {
            switch (_playbackState)
            {
                case StreamingPlaybackState.Stopped:
                    {
                        _playbackState = StreamingPlaybackState.Buffering;
                        _bufferedWaveProvider = null;

                        _cancellationTokenSource?.Dispose();

                        _cancellationTokenSource = new CancellationTokenSource();

                        using (var stream = await _api.StreamTrackAsStream(request))
                        {
                            var task = Task.Run(() => Play(stream, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
                            _timer.Start();
                            await task;
                        }

                        break;
                    }

                case StreamingPlaybackState.Paused:
                    _playbackState = StreamingPlaybackState.Buffering;
                    break;

                case StreamingPlaybackState.Playing:
                    StopPlayback();
                    break;
            }
        }

        private async Task Play(Stream stream, CancellationToken token)
        {
            IMp3FrameDecompressor? decompressor = null;
            var buffer = new byte[16384 * 4]; // needs to be big enough to hold a decompressed frame

            try
            {
                using var responseStream = stream;

                var readFullyStream = new ReadFullyStream(responseStream);
                do
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    if (IsBufferNearlyFull)
                    {
                        Debug.WriteLine("Buffer getting full, taking a break");
                        await Task.Delay(500, token);
                    }
                    else
                    {
                        Mp3Frame? frame;
                        try
                        {
                            frame = Mp3Frame.LoadFromStream(readFullyStream);
                        }
                        catch (EndOfStreamException)
                        {
                            _fullyDownloaded = true;
                            // reached the end of the MP3 file / stream
                            break;
                        }

                        if (frame is null)
                        {
                            break;
                        }

                        if (decompressor is null)
                        {
                            // don't think these details matter too much - just help ACM select the right codec
                            // however, the buffered provider doesn't know what sample rate it is working at
                            // until we have a frame
                            decompressor = CreateFrameDecompressor(frame);
                            _bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat)
                            {
                                BufferDuration = TimeSpan.FromSeconds(20) // allow us to get well ahead of ourselves
                            };
                            //this.bufferedWaveProvider.BufferedDuration = 250;
                        }

                        var decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                        Debug.WriteLine(string.Format("Decompressed a frame {0}", decompressed));
                        _bufferedWaveProvider?.AddSamples(buffer, 0, decompressed);
                    }
                } while (_playbackState != StreamingPlaybackState.Stopped);

                Debug.WriteLine("Exiting");
                // was doing this in a finally block, but for some reason
                // we are hanging on response stream .Dispose so never get there
                decompressor?.Dispose();
            }
            finally
            {
                _bufferedWaveProvider = null;
                decompressor?.Dispose();
            }
        }

        private static IWavePlayer CreateWaveOut()
        {
            return new WaveOut();
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            Debug.WriteLine("Playback Stopped");
            if (e.Exception != null)
            {
                MessageBox.Show(string.Format("Playback Error {0}", e.Exception.Message));
            }
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_playbackState != StreamingPlaybackState.Stopped)
            {
                if (_waveOut == null && _bufferedWaveProvider != null)
                {
                    Debug.WriteLine("Creating WaveOut Device");

                    _waveOut = CreateWaveOut();
                    _waveOut.PlaybackStopped += OnPlaybackStopped;
                    _volumeProvider = new VolumeWaveProvider16(_bufferedWaveProvider)
                    {
                        Volume = 0.5f
                    };
                    _waveOut.Init(_volumeProvider);
                }
                else if (_bufferedWaveProvider != null)
                {
                    var bufferedSeconds = _bufferedWaveProvider.BufferedDuration.TotalSeconds;

                    // make it stutter less if we buffer up a decent amount before playing
                    if (bufferedSeconds < 0.5 && _playbackState == StreamingPlaybackState.Playing && !_fullyDownloaded)
                    {
                        Pause();
                    }
                    else if (bufferedSeconds > 4 && _playbackState == StreamingPlaybackState.Buffering)
                    {
                        Play();
                    }
                    else if (_fullyDownloaded && bufferedSeconds == 0)
                    {
                        Debug.WriteLine("Reached end of stream");
                        StopPlayback();
                    }
                }
            }
        }

        private void Play()
        {
            _waveOut?.Play();
            Debug.WriteLine(string.Format("Started playing, waveOut.PlaybackState={0}", _waveOut?.PlaybackState ?? PlaybackState.Stopped));
            _playbackState = StreamingPlaybackState.Playing;
        }

        private void Pause()
        {
            _playbackState = StreamingPlaybackState.Buffering;
            _waveOut?.Pause();
            Debug.WriteLine(string.Format("Paused to buffer, waveOut.PlaybackState={0}", _waveOut?.PlaybackState ?? PlaybackState.Stopped));
        }

        private void StopPlayback()
        {
            if (_playbackState != StreamingPlaybackState.Stopped)
            {
                _playbackState = StreamingPlaybackState.Stopped;

                _waveOut?.Stop();
                _waveOut?.Dispose();
                _waveOut = null;
            }
        }
    }
}

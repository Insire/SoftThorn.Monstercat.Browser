using NAudio.Wave;
using System;
using System.Threading;
using SoftThorn.Monstercat.Browser.Core;
using Serilog;
using System.IO;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    internal sealed class PlaybackInfrastructure : IDisposable
    {
        private readonly SynchronizationContext _synchronizationContext;

        private static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
        {
            var channelCount = frame.ChannelMode == ChannelMode.Mono
                ? 1
                : 2;
            var waveFormat = new Mp3WaveFormat(frame.SampleRate, channelCount, frame.FrameLength, frame.BitRate);

            return new AcmMp3FrameDecompressor(waveFormat);
        }

        public static PlaybackInfrastructure Create(SynchronizationContext synchronizationContext, ILogger logger, Mp3Frame frame, int volume)
        {
            // don't think these details matter too much - just help ACM select the right codec
            // however, the buffered provider doesn't know what sample rate it is working at
            // until we have a frame
            var decompressor = CreateFrameDecompressor(frame);
            var bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(20) // allow us to get well ahead of ourselves
            };

            var volumeProvider = new VolumeWaveProvider16(bufferedWaveProvider)
            {
                Volume = 0.5f,
            };

            var instance = new PlaybackInfrastructure(synchronizationContext, bufferedWaveProvider, decompressor, volumeProvider, logger);

            instance.Initialize();
            instance.SetVolume(volume);

            return instance;
        }

        private readonly byte[] _buffer;
        private readonly ILogger _logger;

        public BufferedWaveProvider BufferedWaveProvider { get; }
        public IMp3FrameDecompressor FrameDecompressor { get; }
        public VolumeWaveProvider16 VolumeProvider { get; }
        public IWavePlayer? WaveOut { get; private set; }

        public double TotalSeconds => BufferedWaveProvider.BufferedDuration.TotalSeconds;

        private PlaybackInfrastructure(
            SynchronizationContext synchronizationContext,
            BufferedWaveProvider bufferedWaveProvider,
            IMp3FrameDecompressor frameDecompressor,
            VolumeWaveProvider16 volumeProvider,
            ILogger logger)
        {
            _synchronizationContext = synchronizationContext;
            BufferedWaveProvider = bufferedWaveProvider;
            FrameDecompressor = frameDecompressor;
            VolumeProvider = volumeProvider;
            _logger = logger.ForContext<PlaybackInfrastructure>();

            _buffer = new byte[16384 * 4]; // needs to be big enough to hold a decompressed frame
        }

        private void Initialize()
        {
            _synchronizationContext.Send(this, o =>
            {
                o.WaveOut = new WaveOut();
                o.WaveOut.Init(o.VolumeProvider);
            });
        }

        private void AddSamples(int samples)
        {
            BufferedWaveProvider.AddSamples(_buffer, 0, samples);
        }

        private int DecompressFrame(Mp3Frame frame)
        {
            return FrameDecompressor.DecompressFrame(frame, _buffer, 0);
        }

        public static bool TryGetMp3Frame(ReadFullyStream stream, out Mp3Frame frame)
        {
            frame = null!;

            try
            {
                frame = Mp3Frame.LoadFromStream(stream);
            }
            catch (EndOfStreamException)
            {
            }

            if (frame is null)
            {
                return false;
            }

            return true;
        }

        public void ProcessFrame(Mp3Frame frame)
        {
            AddSamples(DecompressFrame(frame));
        }

        public bool IsBufferNearlyFull()
        {
            return BufferedWaveProvider.BufferLength - BufferedWaveProvider.BufferedBytes < BufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4;
        }

        public void SetVolume(int volume)
        {
            if (volume < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (volume > 100)
            {
                throw new ArgumentOutOfRangeException();
            }

            VolumeProvider.Volume = volume / 100f;
        }

        public int GetVolume()
        {
            var volume = VolumeProvider.Volume;
            return Convert.ToInt32(volume * 100);
        }

        public PlaybackState GetState()
        {
            if (WaveOut is null)
            {
                throw new InvalidOperationException();
            }

            return WaveOut.PlaybackState;
        }

        public void Pause()
        {
            if (WaveOut is null)
            {
                throw new InvalidOperationException();
            }

            WaveOut.Pause();
            _logger.Debug("Paused Playback, waveOut.PlaybackState={PlaybackState}", WaveOut.PlaybackState);
        }

        public void Play()
        {
            if (WaveOut is null)
            {
                throw new InvalidOperationException();
            }

            WaveOut.Play();
            _logger.Debug("Started Playback, waveOut.PlaybackState={PlaybackState}", WaveOut.PlaybackState);
        }

        public void Dispose()
        {
            if (WaveOut is null)
            {
                throw new InvalidOperationException();
            }

            WaveOut.Stop();
            _logger.Debug("Stopped Playback, waveOut.PlaybackState={PlaybackState}", WaveOut.PlaybackState);

            WaveOut?.Dispose();
            FrameDecompressor.Dispose();
        }
    }
}

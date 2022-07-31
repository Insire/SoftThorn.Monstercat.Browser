using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using NAudio.Wave;
using Serilog;
using SoftThorn.Monstercat.Browser.Core;
using SoftThorn.MonstercatNet;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    internal sealed class PlaybackService : IPlaybackService
    {
        private readonly SynchronizationContext _synchronizationContext;
        private readonly DispatcherTimer _timer;
        private readonly IMessenger _messenger;
        private readonly ILogger _logger;
        private readonly IMonstercatApi _api;

        private volatile StreamingPlaybackState _playbackState;
        private volatile bool _fullyProcessed;
        private volatile IPlaybackItem? _playbackItem;

        private CancellationTokenSource? _cancellationTokenSource;

        private PlaybackInfrastructure? _playbackInfrastructure;

        public PlaybackService(SynchronizationContext synchronizationContext, DispatcherTimer timer, IMessenger messenger, ILogger logger, IMonstercatApi api)
        {
            _api = api;
            _synchronizationContext = synchronizationContext;
            _timer = timer;
            _messenger = messenger;
            _logger = logger.ForContext<PlaybackService>();
            _timer.Tick += OnTimerTick;
        }

        public StreamingPlaybackState GetPlaybackState()
        {
            return _playbackState;
        }

        public async Task Play(IPlaybackItem item, int volume)
        {
            switch (_playbackState)
            {
                case StreamingPlaybackState.Stopped:
                    {
                        SetPlaybackState(StreamingPlaybackState.Buffering);

                        _cancellationTokenSource?.Dispose();
                        _cancellationTokenSource = new CancellationTokenSource();

                        _logger.Debug("fetching stream");
                        //using (var stream = await _api.StreamTrackAsStream(item.GetStreamRequest()))
                        using (var stream = File.OpenRead(@"C:\Users\peter\OneDrive\Desktop\mixkit-crickets-and-insects-in-the-wild-ambience-39.mp3"))
                        {
                            _playbackItem = item;
                            var task = Task.Run(() => PlayBackLoop(stream, volume, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
                            _timer.Start();
                            await task;
                        }

                        break;
                    }

                case StreamingPlaybackState.Paused:
                    SetPlaybackState(StreamingPlaybackState.Buffering);
                    break;

                case StreamingPlaybackState.Playing:
                    StopPlayback();
                    break;
            }
        }

        private async Task PlayBackLoop(Stream stream, int volume, CancellationToken token)
        {
            _logger.Debug("starting playback");

            try
            {
                using (var readFullyStream = new ReadFullyStream(stream, _logger))
                {
                    PlaybackInfrastructure? infrastructure = null;
                    do
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        if (infrastructure?.IsBufferNearlyFull() == true)
                        {
                            // pause processing
                            await Task.Delay(500, token);
                            continue;
                        }

                        if (!PlaybackInfrastructure.TryGetMp3Frame(readFullyStream, out var frame))
                        {
                            // stop processing
                            break;
                        }

                        if (infrastructure is null)
                        {
                            _playbackInfrastructure = infrastructure = PlaybackInfrastructure.Create(_synchronizationContext, _logger, frame, volume);
                        }

                        infrastructure.ProcessFrame(frame);
                    }
                    while (_playbackState != StreamingPlaybackState.Stopped);

                    _logger.Debug("stream has finished processeing");
                }
            }
            finally
            {
                _fullyProcessed = true;
            }
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_playbackInfrastructure == null)
            {
                return;
            }

            if (_playbackState != StreamingPlaybackState.Buffering)
            {
                switch (_playbackInfrastructure.GetState())
                {
                    case PlaybackState.Stopped:
                        _messenger.Send(new ValueChangedMessage<StreamingPlaybackState>(StreamingPlaybackState.Stopped));
                        break;

                    case PlaybackState.Playing:
                        _messenger.Send(new ValueChangedMessage<StreamingPlaybackState>(StreamingPlaybackState.Playing));
                        break;

                    case PlaybackState.Paused:
                        _messenger.Send(new ValueChangedMessage<StreamingPlaybackState>(StreamingPlaybackState.Paused));
                        break;
                }
            }

            if (_playbackState == StreamingPlaybackState.Stopped || _playbackState == StreamingPlaybackState.Paused)
            {
                return;
            }

            var bufferedSeconds = _playbackInfrastructure.TotalSeconds;

            // make it stutter less if we buffer up a decent amount before playing
            if (bufferedSeconds < 0.5 && _playbackState == StreamingPlaybackState.Playing && !_fullyProcessed)
            {
                _logger.Debug("Pausing to wait for more buffered data");
                Pause();
            }
            else if (bufferedSeconds > 4 && _playbackState == StreamingPlaybackState.Buffering)
            {
                Play();
            }
            else if (_fullyProcessed && bufferedSeconds == 0)
            {
                StopPlayback();
            }
        }

        public void Play()
        {
            _playbackInfrastructure?.Play();
            SetPlaybackState(StreamingPlaybackState.Playing);
        }

        public void Pause()
        {
            _playbackInfrastructure?.Pause();
            SetPlaybackState(StreamingPlaybackState.Paused);
        }

        private void StopPlayback()
        {
            if (_playbackState == StreamingPlaybackState.Playing)
            {
                _playbackInfrastructure?.Dispose();
                _playbackInfrastructure = null;

                _playbackItem?.Dispose();
                _playbackItem = null;

                _timer.Stop();
                SetPlaybackState(StreamingPlaybackState.Stopped);
            }
        }

        private void SetPlaybackState(StreamingPlaybackState state)
        {
            _playbackState = state;

            _logger.Debug("PlaybackState changed to: {StreamingPlaybackState}", state);
            _messenger.Send(new ValueChangedMessage<StreamingPlaybackState>(state));
        }

        public int GetVolume()
        {
            return _playbackInfrastructure?.GetVolume() ?? 50;
        }

        public void SetVolume(int volume)
        {
            _playbackInfrastructure?.SetVolume(volume);
        }
    }
}

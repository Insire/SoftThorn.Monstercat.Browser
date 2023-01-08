using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Serilog;
using SoftThorn.Monstercat.Browser.Core;
using SoftThorn.MonstercatNet;
using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    internal sealed class PlaybackService : IPlaybackService
    {
        private readonly IScheduler _scheduler;
        private readonly DispatcherTimer _timer;
        private readonly IMessenger _messenger;
        private readonly ILogger _logger;
        private readonly IMonstercatApi _api;

        private volatile StreamingPlaybackState _playbackState;
        private volatile bool _fullyProcessed;

        private CancellationTokenSource? _cancellationTokenSource;
        private PlaybackInfrastructure? _playbackInfrastructure;
        private Task _playbackLoop;

        public PlaybackService(IScheduler scheduler, DispatcherTimer timer, IMessenger messenger, ILogger logger, IMonstercatApi api)
        {
            _api = api;
            _scheduler = scheduler;
            _timer = timer;
            _messenger = messenger;
            _logger = logger.ForContext<PlaybackService>();
            _timer.Tick += OnTimerTick;
            _timer.Start();

            _playbackLoop = Task.CompletedTask;
        }

        public StreamingPlaybackState GetPlaybackState()
        {
            return _playbackState;
        }

        public async void Play(IPlaybackItem item, PlaybackIntent intent, int volume)
        {
            switch (_playbackState)
            {
                case StreamingPlaybackState.Stopped:
                    await PlayInternal(item, intent, volume);

                    break;

                case StreamingPlaybackState.Buffering:
                case StreamingPlaybackState.Paused:
                case StreamingPlaybackState.Playing:
                    var task = _playbackLoop;
                    StopPlayback(intent);
                    await task;
                    await PlayInternal(item, intent, volume);

                    break;
            }
        }

        private async Task PlayInternal(IPlaybackItem item, PlaybackIntent intent, int volume)
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            _logger.Debug("fetching stream");

            SetPlaybackState(StreamingPlaybackState.Buffering, intent);

            using (var stream = await _api.StreamTrackAsStream(item.GetStreamRequest(), _cancellationTokenSource.Token))
            {
                _playbackLoop = Task.Run(() => PlayBackLoop(stream, volume, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
                await _playbackLoop;
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
                            _playbackInfrastructure = infrastructure = PlaybackInfrastructure.Create(_scheduler, _logger, frame, volume);
                        }

                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        infrastructure.ProcessFrame(frame);
                    }
                    while (_playbackState != StreamingPlaybackState.Stopped);

                    _logger.Debug("stream has finished processeing");
                }
            }
            catch (TaskCanceledException)
            {
                _logger.Debug("playback has been cancelled");
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

            if (_playbackState == StreamingPlaybackState.Stopped || _playbackState == StreamingPlaybackState.Paused)
            {
                return;
            }

            var bufferedSeconds = _playbackInfrastructure.TotalSeconds;

            // make it stutter less if we buffer up a decent amount before playing
            if (bufferedSeconds < 0.5 && _playbackState == StreamingPlaybackState.Playing && !_fullyProcessed)
            {
                _logger.Debug("Pausing to wait for more buffered data");
                Pause(PlaybackIntent.Auto);
            }
            else if (bufferedSeconds > 4 && _playbackState == StreamingPlaybackState.Buffering)
            {
                _logger.Debug("Resume playback - enough data available");
                Play(PlaybackIntent.Auto);
            }
            else if (_fullyProcessed && bufferedSeconds == 0)
            {
                _logger.Debug("Stop playback - track complete");
                StopPlayback(PlaybackIntent.Auto);
            }
        }

        public void Play(PlaybackIntent intent)
        {
            _playbackInfrastructure?.Play();
            SetPlaybackState(StreamingPlaybackState.Playing, intent);
        }

        public void Pause(PlaybackIntent intent)
        {
            _playbackInfrastructure?.Pause();
            SetPlaybackState(StreamingPlaybackState.Paused, intent);
        }

        public void Stop(PlaybackIntent intent)
        {
            StopPlayback(intent);
        }

        private void StopPlayback(PlaybackIntent intent)
        {
            if (_playbackState == StreamingPlaybackState.Playing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = null;

                _playbackInfrastructure?.Dispose();
                _playbackInfrastructure = null;

                SetPlaybackState(StreamingPlaybackState.Stopped, intent);

                _playbackLoop = Task.CompletedTask;
            }
        }

        private void SetPlaybackState(StreamingPlaybackState state, PlaybackIntent intent, [CallerMemberName] string methodName = null!)
        {
            _playbackState = state;

            _logger.Debug("[{Caller}] PlaybackState changed to: {StreamingPlaybackState} by {Intent}", methodName, state, intent);
            _messenger.Send(new ValueChangedMessage<(StreamingPlaybackState, PlaybackIntent)>((state, intent)));
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

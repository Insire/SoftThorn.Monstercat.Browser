using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DynamicData;
using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class PlaybackViewModel : ObservableObject, IDisposable
    {
        private readonly SourceCache<PlaybackItemViewModel, long> _sourceCache;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly IPlaybackService _playbackService;
        private readonly IMessenger _messenger;
        private readonly IDisposable _subscription;

        private bool _disposedValue;
        private long _currentSequzence;

        public StreamingPlaybackState PlaybackState => _playbackService.GetPlaybackState();

        private bool _isPlaybackAvailable;
        public bool IsPlaybackAvailable
        {
            get { return _isPlaybackAvailable; }
            private set { SetProperty(ref _isPlaybackAvailable, value); }
        }

        private PlaybackItemViewModel? _current;
        public PlaybackItemViewModel? Current
        {
            get { return _current; }
            private set { SetProperty(ref _current, value); }
        }

        private int _volume;
        public int Volume
        {
            get { return _volume; }
            private set { SetProperty(ref _volume, value); }
        }

        public IObservableCollection<PlaybackItemViewModel> Items { get; }

        public PlaybackViewModel(SynchronizationContext synchronizationContext, IPlaybackService playbackService, IMessenger messenger)
        {
            _sourceCache = new SourceCache<PlaybackItemViewModel, long>(vm => vm.Sequence);
            _synchronizationContext = synchronizationContext;
            _playbackService = playbackService;
            _messenger = messenger;

            _currentSequzence = 1;
            _volume = _playbackService.GetVolume();

            _messenger.Register<PlaybackViewModel, ValueChangedMessage<StreamingPlaybackState>>(this, (r, m) => r.OnStreamingPlaybackStateChanged(m.Value));

            Items = new ObservableCollectionExtended<PlaybackItemViewModel>();

            _subscription = _sourceCache.Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .SortBy(p => p.Sequence, SortDirection.Ascending, SortOptimisations.ComparesImmutableValuesOnly)
                .ObserveOn(_synchronizationContext)
                .Bind(Items)
                .DisposeMany()
                .Subscribe();
        }

        public async Task Add(TrackViewModel track)
        {
            var item = new PlaybackItemViewModel(_currentSequzence++, track);
            _sourceCache.AddOrUpdate(item);

            if (CanPlay())
            {
                await Play(new PlaybackItemViewModel(_currentSequzence++, track));
            }
        }

        public async Task Add(IReadOnlyList<TrackViewModel> tracks)
        {
            var items = new List<PlaybackItemViewModel>();
            var firstEntry = default(PlaybackItemViewModel);

            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                var item = new PlaybackItemViewModel(_currentSequzence++, track);

                items.Add(item);

                if (i == 0)
                {
                    firstEntry = item;
                }
            }

            _sourceCache.AddOrUpdate(items);

            if (firstEntry is not null && CanPlay())
            {
                await Play(firstEntry);
            }
        }

        private void OnStreamingPlaybackStateChanged(StreamingPlaybackState state)
        {
            OnPropertyChanged(nameof(PlaybackState));

            switch (state)
            {
                case StreamingPlaybackState.Stopped:

                    var nextEntry = _sourceCache.Items
                         .Where(p => !p.PlaybackCompleted && p != _current)
                         .OrderBy(p => p.Sequence)
                         .FirstOrDefault();

                    if (nextEntry is null)
                    {
                        _synchronizationContext.Post(this, o =>
                        {
                            o.ResumePlayCommand.NotifyCanExecuteChanged();
                            o.NextCommand.NotifyCanExecuteChanged();
                            o.PreviousCommand.NotifyCanExecuteChanged();
                            o.PauseCommand.NotifyCanExecuteChanged();
                        });
                    }
                    else
                    {
                        _ = Play(nextEntry);

                        _synchronizationContext.Post(this, o =>
                        {
                            o.NextCommand.NotifyCanExecuteChanged();
                            o.PreviousCommand.NotifyCanExecuteChanged();
                            o.PauseCommand.NotifyCanExecuteChanged();
                        });
                    }
                    break;

                case StreamingPlaybackState.Playing:
                case StreamingPlaybackState.Buffering:
                case StreamingPlaybackState.Paused:
                    _synchronizationContext.Post(this, o =>
                    {
                        o.ResumePlayCommand.NotifyCanExecuteChanged();
                        o.NextCommand.NotifyCanExecuteChanged();
                        o.PreviousCommand.NotifyCanExecuteChanged();
                        o.PauseCommand.NotifyCanExecuteChanged();
                    });
                    break;
            }
        }

        private async Task Play(PlaybackItemViewModel item)
        {
            Current = item;
            IsPlaybackAvailable = true;

            await _playbackService.Play(item, _volume);
        }

        private bool CanPlay()
        {
            return PlaybackState == StreamingPlaybackState.Stopped;
        }

        [RelayCommand(CanExecute = nameof(CanPause))]
        private void Pause()
        {
            if (CanPause())
            {
                _playbackService.Pause();
            }
        }

        private bool CanPause()
        {
            return PlaybackState == StreamingPlaybackState.Playing;
        }

        [RelayCommand(CanExecute = nameof(CanResumePlay))]
        private void ResumePlay()
        {
            if (CanResumePlay())
            {
                _playbackService.Play();
            }
        }

        private bool CanResumePlay()
        {
            return PlaybackState == StreamingPlaybackState.Paused;
        }

        [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanNext))]
        private async Task Next()
        {
            if (CanNext())
            {
                var nextEntry = _sourceCache.Items
                     .Where(p => !p.PlaybackCompleted && p != _current)
                     .OrderBy(p => p.Sequence)
                     .FirstOrDefault();

                if (nextEntry is not null)
                {
                    await _playbackService.Play(nextEntry, _volume);
                }
            }
        }

        private bool CanNext()
        {
            return _sourceCache.Items
                    .Where(p => !p.PlaybackCompleted && p != _current)
                    .OrderBy(p => p.Sequence)
                    .Any();
        }

        [RelayCommand(CanExecute = nameof(CanPrevious))]
        private void Previous()
        {
            if (CanPrevious())
            {
                _playbackService.Play();
            }
        }

        private bool CanPrevious()
        {
            if (_current?.Sequence is null)
            {
                return false;
            }

            return (_current.Sequence - 1) >= 1;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _subscription.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

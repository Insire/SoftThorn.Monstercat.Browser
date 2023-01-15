using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DynamicData;
using DynamicData.Binding;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class PlaybackViewModel : ObservableObject, IDisposable
    {
        private readonly SourceCache<PlaybackItemViewModel, long> _sourceCache;
        private readonly IScheduler _scheduler;
        private readonly IPlaybackService _playbackService;
        private readonly IMessenger _messenger;
        private readonly IToastService _toastService;
        private readonly SettingsService _settingsService;
        private readonly ILogger _logger;
        private readonly CompositeDisposable _subscription;

        private bool _disposedValue;
        private long _currentSequence;
        private bool _isVolumeLoaded;

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
            private set
            {
                var old = _current;
                if (SetProperty(ref _current, value))
                {
                    if (old is not null)
                    {
                        old.IsCurrentlyPlayed = false;
                    }

                    if (value is not null)
                    {
                        value.IsCurrentlyPlayed = true;
                    }
                }
            }
        }

        [ObservableProperty]
        private int _volume;

        public IObservableCollection<PlaybackItemViewModel> Items { get; }

        public PlaybackViewModel(
            IScheduler scheduler,
            IPlaybackService playbackService,
            IMessenger messenger,
            ILogger logger,
            IToastService toastService,
            SettingsService settingsService)
        {
            _sourceCache = new SourceCache<PlaybackItemViewModel, long>(vm => vm.Sequence);
            _scheduler = scheduler;
            _playbackService = playbackService;
            _messenger = messenger;
            _toastService = toastService;
            _settingsService = settingsService;
            _logger = logger.ForContext<PlaybackViewModel>();

            _currentSequence = 0;

            _messenger.Register<PlaybackViewModel, ValueChangedMessage<(StreamingPlaybackState, PlaybackIntent)>>(this, (r, m) => r.OnStreamingPlaybackStateChanged(m.Value));

            Items = new ObservableCollectionExtended<PlaybackItemViewModel>();

            var currentChanged = this.WhenPropertyChanged(p => p.Current)
                .Where(p => p.Value is null)
                .Select(_ => Unit.Default);

            var countChanged = _sourceCache.CountChanged
                .Where(p => p == 0)
                .Select(_ => Unit.Default);

            _subscription = new CompositeDisposable(new[]
            {
                _sourceCache
                    .Connect()
                    .ObserveOn(TaskPoolScheduler.Default)
                    .DistinctUntilChanged()
                    .SortBy(p => p.Sequence, SortDirection.Ascending, SortOptimisations.ComparesImmutableValuesOnly)
                    .ObserveOn(scheduler)
                    .Bind(Items)
                    .DisposeMany()
                    .Subscribe(),

                this.WhenPropertyChanged(p => p.Volume, notifyOnInitialValue:false)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Throttle(TimeSpan.FromMilliseconds(250))
                    .ObserveOn(scheduler)
                    .Subscribe(p =>
                    {
                        _playbackService.SetVolume(p.Value);
                        settingsService.Volume = p.Value;
                    }),

                currentChanged
                    .CombineLatest(countChanged, (_, __) => Unit.Default)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Throttle(TimeSpan.FromMilliseconds(250))
                    .Subscribe(_ => IsPlaybackAvailable = false)
            });
        }

        public void Add(TrackViewModel track)
        {
            var item = CreatePlaybackItem(track);
            _sourceCache.AddOrUpdate(item);

            if (CanPlay())
            {
                Play(item, PlaybackIntent.User);
            }
        }

        public void Add(IReadOnlyList<TrackViewModel> tracks)
        {
            var items = new List<PlaybackItemViewModel>();
            var firstEntry = default(PlaybackItemViewModel);

            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                var item = CreatePlaybackItem(track, ++_currentSequence);

                items.Add(item);

                if (i == 0)
                {
                    firstEntry = item;
                }
            }

            _sourceCache.AddOrUpdate(items);

            if (firstEntry is not null && CanPlay())
            {
                Play(firstEntry, PlaybackIntent.User);
            }
        }

        private PlaybackItemViewModel CreatePlaybackItem(TrackViewModel track, long? sequence = null)
        {
            if (sequence is null)
            {
                return new PlaybackItemViewModel(++_currentSequence, track);
            }
            else
            {
                return new PlaybackItemViewModel(sequence.Value, track);
            }
        }

        private void OnStreamingPlaybackStateChanged((StreamingPlaybackState State, PlaybackIntent Intent) tuple)
        {
            OnPropertyChanged(nameof(PlaybackState));

            // seamless playback
            if (tuple.Intent == PlaybackIntent.Auto && tuple.State == StreamingPlaybackState.Stopped)
            {
                var current = _current;
                var nextEntry = _sourceCache.Items
                    .Where(p => p != current)
                    .MaxBy(p => p.Sequence);

                if (nextEntry != null)
                {
                    Play(nextEntry, PlaybackIntent.Auto);
                }
                else
                {
                    if (current != null)
                    {
                        Remove(current);
                    }
                }
            }

            _scheduler.Schedule(() =>
            {
                ResumePlayCommand.NotifyCanExecuteChanged();
                NextCommand.NotifyCanExecuteChanged();
                PauseCommand.NotifyCanExecuteChanged();
            });
        }

        private void Play(PlaybackItemViewModel item, PlaybackIntent intent)
        {
            var current = _current;
            if (current is not null)
            {
                _sourceCache.Remove(current);
            }

            if (!_isVolumeLoaded)
            {
                Volume = _settingsService.Volume;
                _isVolumeLoaded = true;
            }

            IsPlaybackAvailable = true;
            Current = item;

            _logger.Debug("Now playing [{Sequence}]{Title} by {Artist} ({ID})", item.Sequence, item.Track.Title, item.Track.ArtistsTitle, item.Track.Id);
            _playbackService.Play(item, intent, _volume);
            _toastService.Show(new ToastViewModel(item.Track.Title, $"by {item.Track.ArtistsTitle}", ToastType.Information, false));
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
                _playbackService.Pause(PlaybackIntent.User);
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
                _playbackService.Play(PlaybackIntent.User);
            }
        }

        private bool CanResumePlay()
        {
            return PlaybackState == StreamingPlaybackState.Paused;
        }

        [RelayCommand(CanExecute = nameof(CanNext))]
        private void Next()
        {
            if (CanNext())
            {
                var nextEntry = _sourceCache.Items
                     .Where(p => p != _current)
                     .MinBy(p => p.Sequence);

                if (nextEntry is not null)
                {
                    Play(nextEntry, PlaybackIntent.User);
                }
            }
        }

        private bool CanNext()
        {
            return _sourceCache.Items
                    .Where(p => p != _current)
                    .OrderBy(p => p.Sequence)
                    .Any();
        }

        [RelayCommand(CanExecute = nameof(CanRemove))]
        private void Remove(object? args)
        {
            if (args is PlaybackItemViewModel item)
            {
                if (Current is not null)
                {
                    var current = Current;
                    if (current == item || current.Track.Id == item.Track.Id)
                    {
                        if (CanNext())
                        {
                            Next();
                        }
                        else
                        {
                            _playbackService.Stop(PlaybackIntent.User);
                        }
                    }
                }

                _sourceCache.Remove(item);
            }
        }

        private static bool CanRemove(object? args)
        {
            return args is PlaybackItemViewModel;
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

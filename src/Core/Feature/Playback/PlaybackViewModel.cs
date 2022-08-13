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
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class PlaybackViewModel : ObservableObject, IDisposable
    {
        private readonly SourceCache<PlaybackItemViewModel, long> _sourceCache;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly IPlaybackService _playbackService;
        private readonly IMessenger _messenger;
        private readonly ILogger _logger;
        private readonly CompositeDisposable _subscription;
        private readonly LinkedList<PlaybackItemViewModel> _history; // replace with linked list

        private bool _disposedValue;
        private long _currentSequence;

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

        private int _volume;
        public int Volume
        {
            get { return _volume; }
            set { SetProperty(ref _volume, value); }
        }

        public IObservableCollection<PlaybackItemViewModel> Items { get; }

        public PlaybackViewModel(SynchronizationContext synchronizationContext, IPlaybackService playbackService, IMessenger messenger, ILogger logger)
        {
            _sourceCache = new SourceCache<PlaybackItemViewModel, long>(vm => vm.Sequence);
            _synchronizationContext = synchronizationContext;
            _playbackService = playbackService;
            _messenger = messenger;
            _logger = logger.ForContext<PlaybackViewModel>();

            _history = new LinkedList<PlaybackItemViewModel>();
            _currentSequence = 0;
            _volume = _playbackService.GetVolume();

            _messenger.Register<PlaybackViewModel, ValueChangedMessage<(StreamingPlaybackState, PlaybackIntent)>>(this, (r, m) => r.OnStreamingPlaybackStateChanged(m.Value));

            Items = new ObservableCollectionExtended<PlaybackItemViewModel>();

            _subscription = new CompositeDisposable(new[]
            {
                _sourceCache
                    .Connect()
                    .ObserveOn(TaskPoolScheduler.Default)
                    .DistinctUntilChanged()
                    .SortBy(p => p.Sequence, SortDirection.Ascending, SortOptimisations.ComparesImmutableValuesOnly)
                    .ObserveOn(_synchronizationContext)
                    .Bind(Items)
                    .DisposeMany()
                    .Subscribe(),

                this.WhenPropertyChanged(p => p.Volume)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Throttle(TimeSpan.FromMilliseconds(250))
                    .ObserveOn(_synchronizationContext)
                    .Subscribe(p => _playbackService.SetVolume(p.Value)),
            });
        }

        public void Add(TrackViewModel track)
        {
            var item = CreatePlaybackItem(track);
            _sourceCache.AddOrUpdate(item);

            if (CanPlay())
            {
                Play(item, PlaybackIntent.User, true);
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
                Play(firstEntry, PlaybackIntent.User, true);
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

            if (tuple.Intent == PlaybackIntent.Auto && tuple.State == StreamingPlaybackState.Stopped)
            {
                var nextEntry = _sourceCache.Items
                 .Where(p => p != _current)
                 .MaxBy(p => p.Sequence);

                if (nextEntry != null)
                {
                    Play(nextEntry, PlaybackIntent.Auto, true);
                }
            }

            _synchronizationContext.Post(this, o =>
            {
                o.ResumePlayCommand.NotifyCanExecuteChanged();
                o.NextCommand.NotifyCanExecuteChanged();
                o.PreviousCommand.NotifyCanExecuteChanged();
                o.PauseCommand.NotifyCanExecuteChanged();
            });
        }

        private void Play(PlaybackItemViewModel item, PlaybackIntent intent, bool trackHistory)
        {
            var current = _current;
            if (current is not null)
            {
                _sourceCache.Remove(current);
            }

            IsPlaybackAvailable = true;
            Current = item;

            if (trackHistory)
            {
                _history.AddLast(item);
            }

            _logger.Debug("Now playing [{Sequence}]{Title} by {Artist} ({ID})", item.Sequence, item.Track.Title, item.Track.ArtistsTitle, item.Track.Id);
            _playbackService.Play(item, intent, _volume);
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
                    Play(nextEntry, PlaybackIntent.User, true);
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

        [RelayCommand(CanExecute = nameof(CanPrevious))]
        private void Previous()
        {
            if (CanPrevious())
            {
                if (_history.Count == 0)
                {
                    return;
                }

                var current = _current;
                var first = _history.First!.Value;

                if (_history.Count == 1)
                {
                    if (current is null) // currently not playing
                    {
                        _sourceCache.AddOrUpdate(first);
                        Play(first, PlaybackIntent.User, false);
                    }
                    else
                    {
                        Play(current, PlaybackIntent.User, false);
                    }

                    return;
                }

                var previous = _history.Last!.Previous!.Value;
                var last = _history.Last.Value;

                if (current is null) // currently not playing
                {
                    _sourceCache.Edit(o =>
                    {
                        // ToDo update all Sequences

                        // the last played entry should already be gone from the SoureCache
                        o.AddOrUpdate(previous);
                        // we immediately go to the previous track and dont reset playback to the last played one

                        _history.RemoveLast();

                        Play(previous, PlaybackIntent.User, false);
                    });
                }
                else
                {
                    _sourceCache.Edit(o =>
                    {
                        // ToDo update all Sequences
                        o.Remove(last);
                        o.AddOrUpdate(previous);
                        o.AddOrUpdate(CreatePlaybackItem(last.Track));

                        _history.RemoveLast();

                        Play(previous, PlaybackIntent.User, false);
                    });
                }
            }
        }

        private bool CanPrevious()
        {
            return _history.Count > 1;
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

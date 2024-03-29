using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class SearchViewModel : ObservableRecipient
    {
        private readonly CompositeDisposable _subscription;

        private readonly SourceCache<TrackViewModel, string> _trackCache;
        private readonly SourceCache<TagViewModel, string> _tagCache;

        private readonly ObservableCollectionExtended<TrackViewModel> _tracks;
        private readonly ObservableCollectionExtended<TagViewModel> _tags;
        private readonly ObservableCollectionExtended<TagViewModel> _selectedTags;

        private bool _disposedValue;

        [ObservableProperty]
        private string? _textFilter;

        public ReadOnlyObservableCollection<TrackViewModel> Tracks { get; }

        public ReadOnlyObservableCollection<TagViewModel> Tags { get; }

        public ReadOnlyObservableCollection<TagViewModel> SelectedTags { get; }

        public Action? OnDownloadStarted { get; set; }

        public SearchViewModel(SynchronizationContext synchronizationContext, IMessenger messenger, IReadOnlyCollection<TrackViewModel> tracks, IReadOnlyCollection<TagViewModel> tags)
            : base(messenger)
        {
            if (synchronizationContext is null)
            {
                throw new ArgumentNullException(nameof(synchronizationContext));
            }

            if (messenger is null)
            {
                throw new ArgumentNullException(nameof(messenger));
            }

            _tagCache = new SourceCache<TagViewModel, string>(vm => vm.Value);
            _tagCache.AddOrUpdate(tags);

            _trackCache = new SourceCache<TrackViewModel, string>(vm => vm.Key);
            _trackCache.AddOrUpdate(tracks);

            _tracks = new ObservableCollectionExtended<TrackViewModel>();
            _tags = new ObservableCollectionExtended<TagViewModel>();
            _selectedTags = new ObservableCollectionExtended<TagViewModel>();

            Tracks = new ReadOnlyObservableCollection<TrackViewModel>(_tracks);
            Tags = new ReadOnlyObservableCollection<TagViewModel>(_tags);
            SelectedTags = new ReadOnlyObservableCollection<TagViewModel>(_selectedTags);

            // observables
            var textFilter = this.WhenPropertyChanged(x => x.TextFilter)
                .DistinctUntilChanged()
                .Throttle(TimeSpan.FromMilliseconds(250))
                .Select(x => BuildTextFilter(x.Value));

            var selectedTagSubscription = _tagCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .AutoRefresh()
                .Filter(x => x.IsSelected)
                .ObserveOn(synchronizationContext)
                .Bind(_selectedTags)
                .Subscribe();

            var tagSubscription = _tagCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .Sort(SortExpressionComparer<TagViewModel>
                    .Ascending(p => p.Value))
                .ObserveOn(synchronizationContext)
                .Bind(_tags, new AddingObservableCollectionAdaptor<TagViewModel, string>())
                .Subscribe();

            var trackSubscription = _trackCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .AutoRefreshOnObservable(_ => _selectedTags.ObserveCollectionChanges(), TimeSpan.FromMilliseconds(250))
                .Filter(textFilter)
                .Filter(BuildTagFilter(_selectedTags))
                .Sort(SortExpressionComparer<TrackViewModel>
                    .Ascending(p => p.Title)
                    .ThenByAscending(p => p.ArtistsTitle)
                    .ThenByAscending(p => p.CatalogId))
                .ObserveOn(synchronizationContext)
                .Bind(_tracks)
                .Subscribe(_ => DownloadCommand.NotifyCanExecuteChanged());

            _subscription = new CompositeDisposable(tagSubscription, trackSubscription, selectedTagSubscription);

            static Func<TrackViewModel, bool> BuildTagFilter(IEnumerable<TagViewModel>? tags)
            {
                return vm =>
                {
                    if (tags?.Any() != true)
                    {
                        return true;
                    }

                    if (vm.Tags is null || vm.Tags.Count == 0)
                    {
                        return false;
                    }

                    return tags.Select(p => p.Value).Intersect(vm.Tags).Any();
                };
            }

            static Func<TrackViewModel, bool> BuildTextFilter(string? searchText)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    return _ => true;
                }

                return vm => vm.Title.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) > -1
                    || vm.ArtistsTitle.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) > -1;
            }
        }

        [RelayCommand(CanExecute = nameof(CanDownload))]
        private void Download()
        {
            Messenger.Send(new DownloadTracksMessage(Tracks.Where(p => p.Downloadable).ToArray()));

            OnDownloadStarted?.Invoke();
        }

        private bool CanDownload()
        {
            return Tracks.Any(p => p.Downloadable);
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

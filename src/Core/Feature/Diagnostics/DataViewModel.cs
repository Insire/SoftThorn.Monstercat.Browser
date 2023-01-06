using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class DataViewModel : ObservableObject, IDisposable
    {
        private readonly CompositeDisposable _subscription;

        private readonly ObservableCollectionExtended<DataViewModelEntry> _tracks;

        [ObservableProperty]
        private string? _textFilter;

        private bool _disposedValue;

        public ReadOnlyObservableCollection<DataViewModelEntry> Tracks { get; }

        public DataViewModel(IScheduler scheduler, ITrackRepository trackRepository)
        {
            _tracks = new ObservableCollectionExtended<DataViewModelEntry>();
            Tracks = new ReadOnlyObservableCollection<DataViewModelEntry>(_tracks);

            // observables
            var textFilter = this.WhenPropertyChanged(x => x.TextFilter)
                .DistinctUntilChanged()
                .Throttle(TimeSpan.FromMilliseconds(250))
                .Select(x => BuildTextFilter(x.Value));

            var trackSubscription = trackRepository
                .ConnectTracks()
                .Filter(textFilter)
                .Sort(SortExpressionComparer<TrackViewModel>
                    .Descending(p => p.ReleaseDate)
                    .ThenByAscending(p => p.ArtistsTitle)
                    .ThenByAscending(p => p.Title)
                    .ThenByAscending(p => p.CatalogId)
                    .ThenByAscending(p => p.Key), SortOptimisations.ComparesImmutableValuesOnly)
                .Transform(p => p.ToViewModel())
                .ObserveOn(scheduler)
                .Bind(_tracks)
                .Subscribe();

            _subscription = new CompositeDisposable(trackSubscription);

            static Func<TrackViewModel, bool> BuildTextFilter(string? searchText)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    return _ => true;
                }

                return vm => Search.Fuzzy(vm, searchText);
            }
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

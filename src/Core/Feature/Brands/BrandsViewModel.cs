using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace SoftThorn.Monstercat.Browser.Core
{
    /// <summary>
    /// top x tracks by brand, sorted by newest
    /// </summary>
    public sealed partial class BrandViewModel<T> : ObservableObject, IDisposable
        where T : Brand, new()
    {
        private readonly Brand _brand;
        private readonly IDisposable _subscription;
        private readonly ObservableCollectionExtended<TrackViewModel> _releases;

        private bool _disposedValue;

        [ObservableProperty]
        private TrackViewModel? _latestRelease;

        public ReadOnlyObservableCollection<TrackViewModel> Releases { get; }

        public BrandViewModel(IScheduler scheduler, ITrackRepository trackRepository)
        {
            _brand = new T();
            _releases = new ObservableCollectionExtended<TrackViewModel>();
            Releases = new ReadOnlyObservableCollection<TrackViewModel>(_releases);

            _subscription = trackRepository
                .ConnectTracks()
                .Filter(p => p.Brand.Equals(_brand.Name, StringComparison.InvariantCultureIgnoreCase))
                .Sort(SortExpressionComparer<TrackViewModel>
                    .Descending(p => p.ReleaseDate)
                    .ThenByAscending(p => p.Title))
                .Do(p =>
                {
                    var vm = p.SortedItems.FirstOrDefault().Value;
                    if (vm != null)
                    {
                        _latestRelease = vm;
                    }
                })
                .ObserveOn(scheduler)
                .Bind(_releases)
                .Subscribe(_ => OnPropertyChanged(nameof(LatestRelease)));
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

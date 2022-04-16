using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Core
{
    /// <summary>
    /// top 15 tracks by brand, sorted by newest
    /// </summary>
    public sealed partial class BrandViewModel<T> : ObservableObject, IDisposable
        where T : Brand, new()
    {
        private readonly Brand _brand;
        private readonly IDisposable _subscription;
        private readonly ObservableCollectionExtended<ReleaseViewModel> _releases;

        private bool _disposedValue;

        [ObservableProperty]
        private ReleaseViewModel? _latestRelease;

        public ReadOnlyObservableCollection<ReleaseViewModel> Releases { get; }

        public BrandViewModel(SynchronizationContext synchronizationContext, TrackRepository trackRepository)
        {
            _brand = new T();
            _releases = new ObservableCollectionExtended<ReleaseViewModel>();
            Releases = new ReadOnlyObservableCollection<ReleaseViewModel>(_releases);

            _subscription = trackRepository
                .ConnectTracks()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .Filter(p => p.Value.Brand.Equals(_brand.Name, StringComparison.InvariantCultureIgnoreCase))
                .Group(p => p.Value.Release.Id)
                .Transform(group =>
                {
                    var key = group.Key.ToString();
                    var cache = group.Cache;

                    var first = cache.Items.FirstOrDefault();
                    if (first.Value is null)
                    {
                        return new ReleaseViewModel();
                    }
                    var release = first.Value.Release;

                    return new ReleaseViewModel()
                    {
                        ArtistsTitle = release.ArtistsTitle,
                        CatalogId = release.CatalogId,
                        Description = release.Description,
                        Id = release.Id,
                        ImageUrl = release.GetSmallCoverArtUri(),
                        ReleaseDate = release.ReleaseDate,
                        Tags = release.Tags,
                        Title = release.Title,
                        Type = release.Type,
                        Upc = release.Upc,
                        Version = release.Version,
                        Tracks = new ObservableCollectionExtended<TrackViewModel>(cache.KeyValues.Select(p => new TrackViewModel()
                        {
                            Id = p.Value.Value.Id,
                            CatalogId = p.Value.Value.Release.CatalogId,
                            ReleaseId = p.Value.Value.Release.Id,
                            DebutDate = p.Value.Value.DebutDate,
                            ReleaseDate = p.Value.Value.Release.ReleaseDate,
                            InEarlyAccess = p.Value.Value.InEarlyAccess,
                            Downloadable = p.Value.Value.Downloadable,
                            Streamable = p.Value.Value.Streamable,
                            Title = p.Value.Value.Title,
                            ArtistsTitle = p.Value.Value.ArtistsTitle,
                            GenreSecondary = p.Value.Value.GenreSecondary,
                            GenrePrimary = p.Value.Value.GenrePrimary,
                            Brand = p.Value.Value.Brand,
                            Version = p.Value.Value.Version,
                            Tags = p.Value.Value.CreateTags(),
                            ImageUrl = p.Value.Value.Release.GetSmallCoverArtUri(),
                        }))
                    };
                })
                .Sort(SortExpressionComparer<ReleaseViewModel>
                    .Descending(p => p.ReleaseDate)
                    .ThenByAscending(p => p.Title))
                .Do(p =>
                {
                    var vm = p.SortedItems.FirstOrDefault().Value;
                    if (vm != null)
                        _latestRelease = vm;
                })
                .ObserveOn(synchronizationContext)
                .Bind(_releases)
                .Subscribe(_ =>
                {
                    OnPropertyChanged(nameof(LatestRelease));
                });
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _subscription.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
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

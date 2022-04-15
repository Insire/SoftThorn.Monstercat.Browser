using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Core
{
    /// <summary>
    /// tracks and releases by newest
    /// </summary>
    [ObservableObject]
    public sealed partial class ReleasesViewModel : IDisposable
    {
        private readonly IDisposable _subscription;
        private readonly ObservableCollectionExtended<TrackViewModel> _tracks;

        private bool _disposedValue;

        public ReadOnlyObservableCollection<TrackViewModel> Tracks { get; }

        public ReleasesViewModel(SynchronizationContext synchronizationContext, TrackRepository trackRepository)
        {
            _tracks = new ObservableCollectionExtended<TrackViewModel>();
            Tracks = new ReadOnlyObservableCollection<TrackViewModel>(_tracks);

            _subscription = trackRepository
                .ConnectTracks()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .Sort(SortExpressionComparer<KeyValuePair<string, Track>>
                    .Descending(p => p.Value.Release.ReleaseDate.Date)
                    .ThenByAscending(p => p.Value.Title))
                .LimitSizeTo(15)
                .Transform(p => new TrackViewModel()
                {
                    Id = p.Value.Id,
                    CatalogId = p.Value.Release.CatalogId,
                    ReleaseId = p.Value.Release.Id,
                    DebutDate = p.Value.DebutDate,
                    ReleaseDate = p.Value.Release.ReleaseDate,
                    InEarlyAccess = p.Value.InEarlyAccess,
                    Downloadable = p.Value.Downloadable,
                    Streamable = p.Value.Streamable,
                    Title = p.Value.Title,
                    ArtistsTitle = p.Value.ArtistsTitle,
                    GenreSecondary = p.Value.GenreSecondary,
                    GenrePrimary = p.Value.GenrePrimary,
                    Brand = p.Value.Brand,
                    Version = p.Value.Version,
                    Tags = p.Value.CreateTags(),
                    ImageUrl = p.Value.Release.GetSmallCoverArtUri(),
                })
                .ObserveOn(synchronizationContext)
                .Bind(_tracks)
                .Subscribe();
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

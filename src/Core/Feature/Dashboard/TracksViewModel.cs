using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using DynamicData.Binding;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class TracksViewModel : ObservableRecipient, IDisposable
    {
        private readonly ObservableCollectionExtended<TrackViewModel> _tracks;

        private bool _disposedValue;
        private IDisposable? _subscription;

        public ReadOnlyObservableCollection<TrackViewModel> Tracks { get; }

        public TracksViewModel(IScheduler scheduler, ITrackRepository trackRepository, IMessenger messenger)
        : base(messenger)
        {
            _tracks = new ObservableCollectionExtended<TrackViewModel>();
            Tracks = new ReadOnlyObservableCollection<TrackViewModel>(_tracks);

            _subscription = CreateSubscription();

            // messages
            Messenger.Register<TracksViewModel, SettingsChangedMessage>(this, (r, m) =>
            {
                r._subscription?.Dispose();
                r._subscription = CreateSubscription(m.Value.TracksCount);
            });

            IDisposable CreateSubscription(int size = 10)
            {
                return trackRepository
                    .ConnectTracks()
                    .Sort(SortExpressionComparer<TrackViewModel>
                        .Descending(p => p.ReleaseDate)
                        .ThenByAscending(p => p.ArtistsTitle)
                        .ThenByAscending(p => p.Title)
                        .ThenByAscending(p => p.CatalogId)
                        .ThenByAscending(p => p.Id), SortOptimisations.ComparesImmutableValuesOnly)
                    .Top(size)
                    .ObserveOn(scheduler)
                    .Bind(_tracks)
                    .Subscribe();
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _subscription?.Dispose();
                    Messenger.UnregisterAll(this);
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

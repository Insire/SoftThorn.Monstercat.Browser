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
    /// <summary>
    /// tracks and releases sorted by newest
    /// </summary>
    public sealed class ReleasesViewModel : ObservableRecipient, IDisposable
    {
        private readonly ObservableCollectionExtended<ReleaseViewModel> _releases;

        private bool _disposedValue;
        private IDisposable? _subscription;

        public ReadOnlyObservableCollection<ReleaseViewModel> Releases { get; }

        public ReleasesViewModel(IScheduler scheduler, ITrackRepository trackRepository, IMessenger messenger)
            : base(messenger)
        {
            _releases = new ObservableCollectionExtended<ReleaseViewModel>();
            Releases = new ReadOnlyObservableCollection<ReleaseViewModel>(_releases);

            _subscription = CreateSubscription();

            // messages
            Messenger.Register<ReleasesViewModel, SettingsChangedMessage>(this, (r, m) =>
            {
                r._subscription?.Dispose();
                r._subscription = CreateSubscription(m.Value.ReleasesCount);
            });

            IDisposable CreateSubscription(int size = 10)
            {
                return trackRepository
                    .ConnectReleases()
                    .Sort(SortExpressionComparer<ReleaseViewModel>
                        .Descending(p => p.ReleaseDate)
                        .ThenByAscending(p => p.ArtistsTitle)
                        .ThenByAscending(p => p.Title)
                        .ThenByAscending(p => p.CatalogId)
                        .ThenByAscending(p => p.Id), SortOptimisations.ComparesImmutableValuesOnly)
                    .Top(size)
                    .ObserveOn(scheduler)
                    .Bind(_releases)
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

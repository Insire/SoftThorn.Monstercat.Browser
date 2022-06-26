using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using DynamicData.Binding;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Core
{
    /// <summary>
    /// top x artists, sorted by newest release
    /// </summary>
    public sealed class ArtistsViewModel : ObservableRecipient, IDisposable
    {
        private readonly ObservableCollectionExtended<ArtistViewModel> _artists;

        private IDisposable? _subscription;
        private bool _disposedValue;

        public ReadOnlyObservableCollection<ArtistViewModel> Artists { get; }

        public ArtistsViewModel(SynchronizationContext synchronizationContext, TrackRepository trackRepository, IMessenger messenger)
            : base(messenger)
        {
            _artists = new ObservableCollectionExtended<ArtistViewModel>();
            Artists = new ReadOnlyObservableCollection<ArtistViewModel>(_artists);

            // messages
            Messenger.Register<ArtistsViewModel, SettingsChangedMessage>(this, (r, m) =>
            {
                r._subscription?.Dispose();
                r._subscription = CreateSubscription(m.Settings.ArtistsCount);
            });

            IDisposable CreateSubscription(int size = 10)
            {
                return trackRepository
                    .ConnectArtists()
                    .Top(SortExpressionComparer<ArtistViewModel>
                        .Descending(p => p.LatestReleaseDate)
                        .ThenByAscending(p => p.Tracks.Count), size)
                    .ObserveOn(synchronizationContext)
                    .Bind(_artists)
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

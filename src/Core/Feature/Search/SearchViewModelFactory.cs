using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class SearchViewModelFactory
    {
        private readonly SynchronizationContext _synchronizationContext;
        private readonly ITrackRepository _trackRepository;
        private readonly IMessenger _messenger;

        public SearchViewModelFactory(SynchronizationContext synchronizationContext, ITrackRepository trackRepository, IMessenger messenger)
        {
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _trackRepository = trackRepository ?? throw new ArgumentNullException(nameof(trackRepository));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public SearchViewModel Create(IReadOnlyCollection<TrackViewModel> tracks, IReadOnlyCollection<TagViewModel> tags)
        {
            return new SearchViewModel(_synchronizationContext, _messenger, tracks, tags);
        }

        public async Task<SearchViewModel> Create()
        {
            var tags = await _trackRepository.ConnectFilteredTagViewModels().FirstOrDefaultAsync();
            var tracks = await _trackRepository.ConnectTracks().FirstOrDefaultAsync();

            return new SearchViewModel(_synchronizationContext, _messenger, tracks.Select(p => p.Current).ToArray(), tags.Select(p => p.Current).ToArray());
        }

        public async Task<SearchViewModel> CreateFromBrand<TBrand>(BrandViewModel<TBrand> brandViewModel)
            where TBrand : Brand, new()
        {
            var tags = await _trackRepository.ConnectFilteredTagViewModels().FirstOrDefaultAsync();

            return new SearchViewModel(_synchronizationContext, _messenger, brandViewModel.Releases.ToArray(), tags.Select(p => p.Current).ToArray());
        }

        public async Task<SearchViewModel> CreateFromRelease(ReleaseViewModel release)
        {
            var tags = await _trackRepository.ConnectFilteredTagViewModels().FirstOrDefaultAsync();

            return new SearchViewModel(_synchronizationContext, _messenger, release.Tracks.ToArray(), tags.Select(p => p.Current).ToArray());
        }

        public async Task<SearchViewModel> CreateFromArtist(ArtistViewModel artist)
        {
            var tags = await _trackRepository.ConnectFilteredTagViewModels().FirstOrDefaultAsync();

            return new SearchViewModel(_synchronizationContext, _messenger, artist.Tracks.ToArray(), tags.Select(p => p.Current).ToArray());
        }
    }
}

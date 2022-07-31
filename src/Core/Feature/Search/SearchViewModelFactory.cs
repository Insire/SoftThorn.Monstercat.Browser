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
        private readonly TrackRepository _trackRepository;
        private readonly IMessenger _messenger;

        public SearchViewModelFactory(SynchronizationContext synchronizationContext, TrackRepository trackRepository, IMessenger messenger)
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
    }
}

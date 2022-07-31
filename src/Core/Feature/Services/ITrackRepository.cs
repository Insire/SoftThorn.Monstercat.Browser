using DynamicData;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public interface ITrackRepository
    {
        IObservable<IChangeSet<ArtistViewModel, Guid>> ConnectArtists();

        IObservable<IChangeSet<TagViewModel, string>> ConnectFilteredTagViewModels();

        IObservable<IChangeSet<KeyValuePair<string, List<TrackViewModel>>, string>> ConnectGenres();

        IObservable<IChangeSet<ReleaseViewModel, Guid>> ConnectReleases();

        IObservable<IChangeSet<KeyValuePair<string, List<TrackViewModel>>, string>> ConnectTags();

        IObservable<IChangeSet<TrackViewModel, string>> ConnectTracks();

        IObservable<IChangeSet<TagViewModel, string>> ConnectUnfilteredTagViewModels();

        void Dispose();

        Task Refresh();
    }
}

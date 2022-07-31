using DynamicData;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class MockTrackRepository : ITrackRepository
    {
        public IObservable<IChangeSet<ArtistViewModel, Guid>> ConnectArtists()
        {
            return new List<IChangeSet<ArtistViewModel, Guid>>()
            {
                new ChangeSet<ArtistViewModel,Guid>(new[]
                {
                    new Change<ArtistViewModel,Guid>(ChangeReason.Add,Guid.Empty, new ArtistViewModel()
                    {
                        Id = Guid.Empty,
                        LatestReleaseCount = 1,
                        LatestReleaseDate = DateTime.MinValue,
                        Name = "Test",
                        Uri = string.Empty,
                        Tracks = new System.Collections.ObjectModel.ObservableCollection<TrackViewModel>()
                        {
                            new TrackViewModel()
                            {
                                Id = Guid.Empty,
                                ArtistsTitle = "Test",
                                Brand = "Test",
                                CatalogId = "Test",
                                DebutDate = null,
                                Downloadable = false,
                                GenrePrimary = "TestPrimary",
                                GenreSecondary = "TestSecondary",
                                ImageUrl = null,
                                InEarlyAccess =false,
                                Key = "1",
                                Release = new ReleaseViewModel(),
                                ReleaseDate = DateTime.MinValue,
                                Streamable = true,
                                Tags = new System.Collections.ObjectModel.ObservableCollection<string>(),
                                Title = "Test",
                                Type = "Test",
                                Version = string.Empty,
                            }
                        }
                    }),
                }),
            }.ToObservable();
        }

        public IObservable<IChangeSet<TagViewModel, string>> ConnectFilteredTagViewModels()
        {
            return new List<IChangeSet<TagViewModel, string>>().ToObservable();
        }

        public IObservable<IChangeSet<KeyValuePair<string, List<TrackViewModel>>, string>> ConnectGenres()
        {
            return new List<IChangeSet<KeyValuePair<string, List<TrackViewModel>>, string>>().ToObservable();
        }

        public IObservable<IChangeSet<ReleaseViewModel, Guid>> ConnectReleases()
        {
            return new List<IChangeSet<ReleaseViewModel, Guid>>().ToObservable();
        }

        public IObservable<IChangeSet<KeyValuePair<string, List<TrackViewModel>>, string>> ConnectTags()
        {
            return new List<IChangeSet<KeyValuePair<string, List<TrackViewModel>>, string>>().ToObservable();
        }

        public IObservable<IChangeSet<TrackViewModel, string>> ConnectTracks()
        {
            return new List<IChangeSet<TrackViewModel, string>>().ToObservable();
        }

        public IObservable<IChangeSet<TagViewModel, string>> ConnectUnfilteredTagViewModels()
        {
            return new List<IChangeSet<TagViewModel, string>>().ToObservable();
        }

        public void Dispose()
        {
        }

        public Task Refresh()
        {
            return Task.CompletedTask;
        }
    }
}

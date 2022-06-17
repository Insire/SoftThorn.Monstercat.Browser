using DynamicData.Binding;
using SoftThorn.MonstercatNet;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SoftThorn.Monstercat.Browser.Core
{
    internal static class MonstercatExtensions
    {
        public static ObservableCollection<string> CreateTags(this Track track)
        {
            var collection = new ObservableCollection<string>(track.Tags ?? Enumerable.Empty<string>());
            if (!string.IsNullOrWhiteSpace(track.GenrePrimary))
            {
                collection.Add(track.GenrePrimary.Trim().ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(track.GenreSecondary))
            {
                collection.Add(track.GenreSecondary.Trim().ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(track.Brand))
            {
                collection.Add(track.Brand.Trim().ToLowerInvariant());
            }

            return collection;
        }

        public static TrackViewModel ToViewModel(this KeyValuePair<string, Track> pair)
        {
            return pair.Value.ToViewModel();
        }

        public static TrackViewModel ToViewModel(this Track track)
        {
            return new TrackViewModel()
            {
                Key = $"{track.Release.Id}_{track.Id}",
                Id = track.Id,
                CatalogId = track.Release.CatalogId,
                DebutDate = track.DebutDate,
                ReleaseDate = track.Release.ReleaseDate,
                InEarlyAccess = track.InEarlyAccess,
                Downloadable = track.Downloadable,
                Streamable = track.Streamable,
                Title = track.Title,
                ArtistsTitle = track.ArtistsTitle,
                GenreSecondary = track.GenreSecondary,
                GenrePrimary = track.GenrePrimary,
                Brand = track.Brand,
                Version = track.Version,
                Tags = track.CreateTags(),
                ImageUrl = track.Release.GetSmallCoverArtUri(),
            };
        }

        public static ReleaseViewModel ToViewModel(this TrackRelease release)
        {
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
                Tracks = new ObservableCollectionExtended<TrackViewModel>(),
            };
        }

        public static ArtistViewModel ToViewModel(this Artist artist)
        {
            return new ArtistViewModel()
            {
                Id = artist.ArtistId,
                Name = artist.Name,
                Uri = artist.Uri,
                Tracks = new ObservableCollectionExtended<TrackViewModel>(),
            };
        }
    }
}

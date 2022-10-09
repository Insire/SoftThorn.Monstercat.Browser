using DynamicData.Binding;
using Microsoft.Extensions.ObjectPool;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

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

        public static TrackViewModel ToViewModel(this KeyValuePair<string, Track> pair, ObjectPool<StringBuilder> objectPool)
        {
            return pair.Value.ToViewModel(objectPool);
        }

        public static TrackViewModel ToViewModel(this Track track, ObjectPool<StringBuilder> objectPool)
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
                Release = track.Release.ToViewModel(),
                FileName = track.GetFileName(objectPool),
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

        public static ArtistViewModel ToViewModel(this TrackArtist artist)
        {
            return new ArtistViewModel()
            {
                Id = artist.Id,
                Name = artist.Name,
                Uri = artist.GetSmallArtistPhotoUri().ToString(),
                Tracks = new ObservableCollectionExtended<TrackViewModel>(),
            };
        }

        internal static string GetFileName(this Track track, ObjectPool<StringBuilder> objectPool)
        {
            var builder = objectPool.Get();
            try
            {
                builder.Clear();
                builder.Append(track.ArtistsTitle);
                builder.Append(" - ");
                builder.Append(track.Title);

                if (!string.IsNullOrWhiteSpace(track.Version))
                {
                    builder.Append('(');
                    builder.Append(track.Version);
                    builder.Append(')');
                }

                return builder.ToString().SanitizeAsFileName();
            }
            finally
            {
                builder.Clear();
                objectPool.Return(builder);
            }
        }

        internal static string GetFilePath(this TrackViewModel track, StringBuilder builder, string downloadPath, FileFormat fileFormat)
        {
            builder.Clear();
            builder.Append(track.FileName);

            builder.Append(GetFileExtension(fileFormat));

            var fileName = builder.ToString().SanitizeAsFileName();

            return Path.Combine(downloadPath, fileName!);
        }

        internal static string GetFileExtension(this FileFormat fileFormat)
        {
            return fileFormat switch
            {
                FileFormat.flac => ".flac",
                FileFormat.mp3 => ".mp3",
                FileFormat.wav => ".wav",
                _ => throw new NotImplementedException(),
            };
        }
    }
}

using SoftThorn.MonstercatNet;
using System;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class SettingsModel
    {
        public string? DownloadTracksPath { get; init; }

        public string? DownloadImagesPath { get; init; }

        public FileFormat DownloadFileFormat { get; init; }

        public int ParallelDownloads { get; init; }

        public string[] ExcludedTags { get; init; } = Array.Empty<string>();

        public int ArtistsCount { get; init; }

        public int GenresCount { get; init; }

        public int ReleasesCount { get; init; }

        public int TagsCount { get; init; }
    }
}

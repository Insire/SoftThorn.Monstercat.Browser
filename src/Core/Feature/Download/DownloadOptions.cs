using SoftThorn.MonstercatNet;
using System.Collections.Generic;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class DownloadOptions
    {
        required public int ParallelDownloads { get; init; }

        required public string DownloadTracksPath { get; init; }

        required public FileFormat DownloadFileFormat { get; init; }

        required public IReadOnlyCollection<TrackViewModel> Tracks { get; init; }
    }
}

using System;
using System.Collections.Generic;

namespace SoftThorn.Monstercat.Browser.Core
{
    internal sealed class DownloadTracksMessage
    {
        public IReadOnlyCollection<TrackViewModel> Tracks { get; init; } = Array.Empty<TrackViewModel>();
    }
}

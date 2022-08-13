using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.Generic;

namespace SoftThorn.Monstercat.Browser.Core
{
    internal sealed class DownloadTracksMessage : ValueChangedMessage<IReadOnlyCollection<TrackViewModel>>
    {
        public DownloadTracksMessage(IReadOnlyCollection<TrackViewModel> tracks)
            : base(tracks)
        {
        }
    }
}

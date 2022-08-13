using SoftThorn.MonstercatNet;

namespace SoftThorn.Monstercat.Browser.Core
{
    public interface IPlaybackItem
    {
        TrackStreamRequest GetStreamRequest();
    }
}

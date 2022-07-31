using SoftThorn.MonstercatNet;
using System;

namespace SoftThorn.Monstercat.Browser.Core
{
    public interface IPlaybackItem : IDisposable
    {
        TrackStreamRequest GetStreamRequest();
    }
}

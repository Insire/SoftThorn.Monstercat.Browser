using SoftThorn.Monstercat.Browser.Core;
using SoftThorn.MonstercatNet;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Avalonia
{
    internal sealed class PlaybackService : IPlaybackService
    {
        public Task Play(TrackStreamRequest request)
        {
            return Task.CompletedTask;
        }
    }
}

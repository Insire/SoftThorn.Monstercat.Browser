using SoftThorn.MonstercatNet;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public interface IPlaybackService
    {
        Task Play(TrackStreamRequest request);
    }
}

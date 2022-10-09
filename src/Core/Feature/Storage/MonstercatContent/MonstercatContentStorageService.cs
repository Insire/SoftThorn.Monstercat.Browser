using Serilog;
using SoftThorn.MonstercatNet;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class MonstercatContentStorageService : FileStorageService<TrackSearchResult>
    {
        public MonstercatContentStorageService(ILogger logger)
            : base(logger?.ForContext<MonstercatContentStorageService>())
        {
        }
    }
}

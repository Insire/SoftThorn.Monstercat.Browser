using SoftThorn.MonstercatNet;
using System.Collections.ObjectModel;
using System.Linq;

namespace SoftThorn.Monstercat.Browser.Core
{
    internal static class MonstercatExtensions
    {
        public static ObservableCollection<string> CreateTags(this Track track)
        {
            var collection = new ObservableCollection<string>(track.Tags ?? Enumerable.Empty<string>());
            if (!string.IsNullOrWhiteSpace(track.GenrePrimary))
            {
                collection.Add(track.GenrePrimary);
            }

            if (!string.IsNullOrWhiteSpace(track.GenreSecondary))
            {
                collection.Add(track.GenreSecondary);
            }

            if (!string.IsNullOrWhiteSpace(track.Brand))
            {
                collection.Add(track.Brand);
            }

            return collection;
        }
    }
}

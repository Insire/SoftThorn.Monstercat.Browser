using FuzzySharp;
using FuzzySharp.PreProcess;
using System;
using System.Linq;

namespace SoftThorn.Monstercat.Browser.Core
{
    internal static class Search
    {
        public static bool Fuzzy(TrackViewModel track, string input)
        {
            const int RATIO = 75;

            if (Fuzz.PartialRatio(track.Title, input, PreprocessMode.Full) > RATIO)
            {
                return true;
            }

            if (track.Release.CatalogId.IndexOf(input, StringComparison.OrdinalIgnoreCase) > -1)
            {
                return true;
            }

            if (Fuzz.PartialRatio(track.ArtistsTitle, input, PreprocessMode.Full) > RATIO)
            {
                return true;
            }

            if (Fuzz.PartialRatio(track.Release.ArtistsTitle, input, PreprocessMode.Full) > RATIO)
            {
                return true;
            }

            if (Fuzz.PartialRatio(track.Release.Title, input, PreprocessMode.Full) > RATIO)
            {
                return true;
            }

            if (track.Brand.IndexOf(input, StringComparison.OrdinalIgnoreCase) > -1)
            {
                return true;
            }

            if (track.GenrePrimary.IndexOf(input, StringComparison.OrdinalIgnoreCase) > -1)
            {
                return true;
            }

            if (track.GenreSecondary.IndexOf(input, StringComparison.OrdinalIgnoreCase) > -1)
            {
                return true;
            }

            if ((track.Type ?? string.Empty).IndexOf(input, StringComparison.OrdinalIgnoreCase) > -1)
            {
                return true;
            }

            if (track.Version.IndexOf(input, StringComparison.OrdinalIgnoreCase) > -1)
            {
                return true;
            }

            if (track.Release.Type.IndexOf(input, StringComparison.OrdinalIgnoreCase) > -1)
            {
                return true;
            }

            if (Fuzz.PartialRatio(track.Release.Description, input, PreprocessMode.Full) > RATIO)
            {
                return true;
            }

            if (track.Release?.Tags?.Any(p => Fuzz.PartialRatio(p, input, PreprocessMode.Full) > RATIO) == true)
            {
                return true;
            }

            if (track.Tags?.Any(p => Fuzz.PartialRatio(p, input, PreprocessMode.Full) > RATIO) == true)
            {
                return true;
            }

            return false;
        }
    }
}

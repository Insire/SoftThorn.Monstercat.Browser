using System.IO;
using System.Text.RegularExpressions;

namespace SoftThorn.Monstercat.Browser.Core
{
    public static class StringExtensions
    {
        // https://msdn.microsoft.com/en-us/library/aa365247.aspx#naming_conventions
        // http://stackoverflow.com/questions/146134/how-to-remove-illegal-characters-from-path-and-filenames
        private static readonly Regex removeInvalidChars = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]",
            RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string? SanitizeAsFileName(this string? fileName, string replacement = "_")
        {
            if (fileName is null)
            {
                return null;
            }

            var result = removeInvalidChars.Replace(fileName, replacement);

            while (result.IndexOf("__") >= 0)
            {
                result = result.Replace("__", replacement);
            }

            return result;
        }
    }
}

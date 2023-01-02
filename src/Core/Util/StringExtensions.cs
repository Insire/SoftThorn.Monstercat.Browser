using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public static class StringExtensions
    {
        // https://msdn.microsoft.com/en-us/library/aa365247.aspx#naming_conventions
        // http://stackoverflow.com/questions/146134/how-to-remove-illegal-characters-from-path-and-filenames
        private static readonly Regex _removeInvalidChars = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]",
            RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string SanitizeAsFileName(this string? fileName, string replacement = "_")
        {
            if (fileName is null)
            {
                return string.Empty;
            }

            var result = _removeInvalidChars.Replace(fileName, replacement);

            while (result.IndexOf("__") >= 0)
            {
                result = result.Replace("__", replacement);
            }

            return result;
        }

        public static async Task<string> CalculateMd5(this Stream data, CancellationToken token = default)
        {
            using (var instance = MD5.Create())
            {
                var hashBytes = await instance.ComputeHashAsync(data, token).ConfigureAwait(false);

                return Convert.ToHexString(hashBytes);
            }
        }
    }
}

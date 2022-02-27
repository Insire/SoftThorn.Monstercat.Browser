using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Microsoft.IO;

namespace SoftThorn.Monstercat.Browser.Avalonia
{
    /// <summary>
    /// Provides memory and disk cached way to asynchronously load images for <see cref="ImageLoader"/>
    /// Can be used as base class if you want to create custom caching mechanism
    /// </summary>
    public class DiskCachedWebImageLoader : RamCachedWebImageLoader
    {
        private readonly string _cacheFolder;

        public DiskCachedWebImageLoader(RecyclableMemoryStreamManager streamManager, string cacheFolder = "Cache/Images/")
            : base(streamManager)
        {
            _cacheFolder = cacheFolder;
        }

        public DiskCachedWebImageLoader(HttpClient httpClient, RecyclableMemoryStreamManager streamManager, bool disposeHttpClient, string cacheFolder = "Cache/Images/")
            : base(httpClient, streamManager, disposeHttpClient)
        {
            _cacheFolder = cacheFolder;
        }

        /// <inheritdoc />
        protected override async Task<Bitmap?> LoadFromGlobalCache(Uri? url)
        {
            if (url is null)
            {
                return null;
            }

            var md5 = await CreateMD5(url);
            var path = Path.Combine(_cacheFolder, md5);
            if (!File.Exists(path))
            {
                return null;
            }

            return new Bitmap(path);
        }

        protected override async Task SaveToGlobalCache(Uri? url, Stream imageBytes)
        {
            var md5 = await CreateMD5(url);
            var path = Path.Combine(_cacheFolder, md5);

            imageBytes.Seek(0, SeekOrigin.Begin);
            using var fileStream = File.OpenWrite(path);
            await imageBytes.CopyToAsync(fileStream);
        }

        protected Task<string> CreateMD5(Uri? input)
        {
            if (input is null)
            {
                return Task.FromResult(string.Empty);
            }

            using var resultStream = StreamManager.GetStream();
            var bytes = Encoding.ASCII.GetBytes(input.OriginalString);
            resultStream.Write(bytes, 0, bytes.Length);
            resultStream.Seek(0, SeekOrigin.Begin);

            return Calculate(resultStream);

            static async Task<string> Calculate(Stream data)
            {
                using (var instance = MD5.Create())
                {
                    var hashBytes = await instance.ComputeHashAsync(data).ConfigureAwait(false);

                    return Convert.ToHexString(hashBytes);
                }
            }
        }
    }
}

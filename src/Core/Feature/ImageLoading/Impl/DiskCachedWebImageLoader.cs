using Microsoft.Extensions.Caching.Memory;
using Microsoft.IO;
using Serilog;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    /// <summary>
    /// Provides memory and disk cached way to asynchronously load images for <see cref="ImageLoader"/>
    /// Can be used as base class if you want to create custom caching mechanism
    /// </summary>
    public class DiskCachedWebImageLoader<T> : RamCachedWebImageLoader<T>
        where T : class
    {
        private readonly string _cacheFolder;

        public DiskCachedWebImageLoader(ILogger log, MemoryCache memoryCache, RecyclableMemoryStreamManager streamManager, IImageFactory<T> imageFactory, string cacheFolder = "Cache/Images/", bool createFolder = false)
            : base(log.ForContext<DiskCachedWebImageLoader<T>>(), memoryCache, streamManager, imageFactory)
        {
            _cacheFolder = cacheFolder;

            if (createFolder)
            {
                Directory.CreateDirectory(cacheFolder);
            }
        }

        public DiskCachedWebImageLoader(ILogger log, MemoryCache memoryCache, HttpClient httpClient, RecyclableMemoryStreamManager streamManager, IImageFactory<T> imageFactory, bool disposeHttpClient, string cacheFolder = "Cache/Images/", bool createFolder = false)
            : base(log.ForContext<DiskCachedWebImageLoader<T>>(), httpClient, memoryCache, streamManager, imageFactory, disposeHttpClient)
        {
            _cacheFolder = cacheFolder;

            if (createFolder)
            {
                Directory.CreateDirectory(cacheFolder);
            }
        }

        /// <inheritdoc />
        protected override async Task<T?> LoadFromGlobalCache(Uri? url)
        {
            if (url is null)
            {
                return null;
            }

            var md5 = await CreateMD5(url);
            var path = Path.Combine(_cacheFolder, md5);
            if (!File.Exists(path))
            {
                Log.Verbose("[CACHEMISS] for {Url}", url);
                return null;
            }

            return await Task.Run(() => ImageFactory.From(path));
        }

        protected override async Task SaveToGlobalCache(Uri? url, Stream imageBytes)
        {
            var md5 = await CreateMD5(url);
            var path = Path.Combine(_cacheFolder, md5);

            imageBytes.Seek(0, SeekOrigin.Begin);

            using var fileStream = File.OpenWrite(path);
            await imageBytes.CopyToAsync(fileStream);
        }

        protected async Task<string> CreateMD5(Uri? input)
        {
            if (input is null)
            {
                return string.Empty;
            }

            using var resultStream = StreamManager.GetStream();
            var bytes = Encoding.ASCII.GetBytes(input.OriginalString);
            resultStream.Write(bytes, 0, bytes.Length);
            resultStream.Seek(0, SeekOrigin.Begin);

            return await Calculate(resultStream);
        }

        private static async Task<string> Calculate(Stream data)
        {
            using (var instance = MD5.Create())
            {
                var hashBytes = await instance.ComputeHashAsync(data).ConfigureAwait(false);

                return Convert.ToHexString(hashBytes);
            }
        }
    }
}

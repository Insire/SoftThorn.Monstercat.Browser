using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Microsoft.IO;

namespace SoftThorn.Monstercat.Browser.Avalonia
{
    /// <summary>
    /// Provides memory cached way to asynchronously load images for <see cref="ImageLoader"/>
    /// Can be used as base class if you want to create custom in memory caching
    /// </summary>
    public class RamCachedWebImageLoader : BaseWebImageLoader
    {
        private readonly ConcurrentDictionary<Uri, Task<IBitmap?>> _memoryCache = new();

        /// <inheritdoc />
        public RamCachedWebImageLoader(RecyclableMemoryStreamManager streamManager)
            : base(streamManager)
        {
        }

        /// <inheritdoc />
        public RamCachedWebImageLoader(HttpClient httpClient, RecyclableMemoryStreamManager streamManager, bool disposeHttpClient)
            : base(httpClient, streamManager, disposeHttpClient) { }

        /// <inheritdoc />
        public override async Task<IBitmap?> ProvideImageAsync(Uri? url)
        {
            if (url is null)
            {
                return null;
            }

            var bitmap = await _memoryCache.GetOrAdd(url, LoadAsync);
            // If load failed - remove from cache and return
            // Next load attempt will try to load image again
            if (bitmap == null)
            {
                _memoryCache.TryRemove(url, out _);
            }

            return bitmap;
        }
    }
}

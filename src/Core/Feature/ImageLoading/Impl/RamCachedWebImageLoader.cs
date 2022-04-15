using Microsoft.IO;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    /// <summary>
    /// Provides memory cached way to asynchronously load images for <see cref="ImageLoader"/>
    /// Can be used as base class if you want to create custom in memory caching
    /// </summary>
    public class RamCachedWebImageLoader<T> : BaseWebImageLoader<T>
        where T : class
    {
        private readonly ConcurrentDictionary<Uri, Task<T?>> _memoryCache = new();

        /// <inheritdoc />
        public RamCachedWebImageLoader(RecyclableMemoryStreamManager streamManager, IImageFactory<T> imageFactory)
            : base(streamManager, imageFactory)
        {
        }

        /// <inheritdoc />
        public RamCachedWebImageLoader(HttpClient httpClient, RecyclableMemoryStreamManager streamManager, IImageFactory<T> imageFactory, bool disposeHttpClient)
            : base(httpClient, streamManager, imageFactory, disposeHttpClient) { }

        /// <inheritdoc />
        public override async Task<T?> ProvideImageAsync(Uri? url)
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

using Microsoft.Extensions.Caching.Memory;
using Microsoft.IO;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
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
        private readonly MemoryCache _cache;
        private readonly ConcurrentDictionary<object, SemaphoreSlim> _locks;

        public RamCachedWebImageLoader(ILogger log, MemoryCache memoryCache, RecyclableMemoryStreamManager streamManager, IImageFactory<T> imageFactory)
            : base(log.ForContext<RamCachedWebImageLoader<T>>(), streamManager, imageFactory)
        {
            _cache = memoryCache;
            _locks = new ConcurrentDictionary<object, SemaphoreSlim>();
        }

        public RamCachedWebImageLoader(ILogger log, HttpClient httpClient, MemoryCache memoryCache, RecyclableMemoryStreamManager streamManager, IImageFactory<T> imageFactory, bool disposeHttpClient)
            : base(log.ForContext<RamCachedWebImageLoader<T>>(), httpClient, streamManager, imageFactory, disposeHttpClient)
        {
            _cache = memoryCache;
            _locks = new ConcurrentDictionary<object, SemaphoreSlim>();
        }

        /// <inheritdoc />
        public override Task<T?> ProvideImageAsync(Uri? url)
        {
            if (url is null)
            {
                return Task.FromResult<T?>(null);
            }

            return GetOrCreate(url, () => LoadAsync(url));
        }

        private async Task<T?> GetOrCreate(object key, Func<Task<T?>> createItem)
        {
            T? cacheEntry;

            if (!_cache.TryGetValue(key, out cacheEntry))// Look for cache key.
            {
                var currentLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

                await currentLock.WaitAsync();
                try
                {
                    if (!_cache.TryGetValue(key, out cacheEntry))
                    {
                        // Key not in cache, so get data.
                        cacheEntry = await createItem();

                        // If load failed - remove from cache and return
                        // Next load attempt will try to load image again
                        if (cacheEntry is null)
                        {
                            return null;
                        }

                        // we rely on the frontend keeping a reference to the provided image,
                        // that way we can remove the image from our internal cache
                        // after a generous delay any parallel requests should be complete
                        _cache.Set(key, cacheEntry, DateTimeOffset.UtcNow.AddSeconds(35));
                    }
                }
                finally
                {
                    currentLock.Release();
                }
            }

            return cacheEntry;
        }
    }
}

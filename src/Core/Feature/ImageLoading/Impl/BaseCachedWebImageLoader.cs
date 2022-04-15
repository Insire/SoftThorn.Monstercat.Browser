using Microsoft.IO;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    /// <summary>
    /// Provides non cached way to asynchronously load images for <see cref="ImageLoader"/>
    /// Can be used as base class if you want to create custom caching mechanism
    /// </summary>
    public class BaseWebImageLoader<T> : IAsyncImageLoader<T>
        where T : class
    {
        private readonly bool _shouldDisposeHttpClient;

        protected HttpClient HttpClient { get; }

        protected RecyclableMemoryStreamManager StreamManager { get; }
        public IImageFactory<T> ImageFactory { get; }

        /// <summary>
        /// Initializes a new instance with new <see cref="HttpClient"/> instance
        /// </summary>
        public BaseWebImageLoader(RecyclableMemoryStreamManager streamManager, IImageFactory<T> imageFactory)
            : this(new HttpClient(), streamManager, imageFactory, true)
        {
        }

        /// <summary>
        /// Initializes a new instance with the provided <see cref="HttpClient"/>, and specifies whether that <see cref="HttpClient"/> should be disposed when this instance is disposed.
        /// </summary>
        /// <param name="httpClient">The HttpMessageHandler responsible for processing the HTTP response messages.</param>
        /// <param name="disposeHttpClient">true if the inner handler should be disposed of by Dispose; false if you intend to reuse the HttpClient.</param>
        public BaseWebImageLoader(HttpClient httpClient, RecyclableMemoryStreamManager streamManager, IImageFactory<T> imageFactory, bool disposeHttpClient)
        {
            HttpClient = httpClient;
            StreamManager = streamManager;
            ImageFactory = imageFactory;
            _shouldDisposeHttpClient = disposeHttpClient;
        }

        /// <inheritdoc />
        public virtual Task<T?> ProvideImageAsync(Uri? url)
        {
            return LoadAsync(url);
        }

        /// <summary>
        /// Attempts to load bitmap
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Bitmap</returns>
        protected virtual async Task<T?> LoadAsync(Uri? url)
        {
            var internalOrCachedBitmap = await LoadFromInternalAsync(url) ?? await LoadFromGlobalCache(url);
            if (internalOrCachedBitmap is not null)
            {
                return internalOrCachedBitmap;
            }

            try
            {
                var externalBytes = await LoadDataFromExternalAsync(url);
                if (externalBytes is null)
                {
                    return null;
                }

                using (externalBytes)
                {
                    var bitmap = ImageFactory.From(externalBytes);

                    await SaveToGlobalCache(url, externalBytes);
                    return bitmap;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Receives image bytes from an internal source (for example, from the disk).
        /// This data will be NOT cached globally (because it is assumed that it is already in internal source us and does not require global caching)
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Bitmap</returns>
        protected virtual Task<T?> LoadFromInternalAsync(Uri? url)
        {
            if (url?.Scheme.Equals("HTTPS", StringComparison.InvariantCultureIgnoreCase) != false || url.Scheme.Equals("HTTP", StringComparison.InvariantCultureIgnoreCase))
            {
                return Task.FromResult<T?>(null);
            }

            try
            {
                return Task.FromResult(ImageFactory.From(url))!;
            }
            catch
            {
                return Task.FromResult<T?>(null);
            }
        }

        /// <summary>
        /// Receives image bytes from an external source (for example, from the Internet).
        /// This data will be cached globally (if required by the current implementation)
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Image bytes</returns>
        protected virtual async Task<Stream?> LoadDataFromExternalAsync(Uri? url)
        {
            if (url is null)
            {
                return null;
            }

            try
            {
                var resultStream = StreamManager.GetStream();

                using (var stream = await HttpClient.GetStreamAsync(url))
                {
                    await stream.CopyToAsync(resultStream);

                    resultStream.Seek(0, SeekOrigin.Begin);
                    return resultStream;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to load image from global cache (if it is stored before)
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Bitmap</returns>
        protected virtual Task<T?> LoadFromGlobalCache(Uri? url)
        {
            // Current implementation does not provide global caching
            return Task.FromResult<T?>(null);
        }

        /// <summary>
        /// Attempts to load image from global cache (if it is stored before)
        /// </summary>
        /// <param name="url">Target url</param>
        /// <param name="imageBytes">Bytes to save</param>
        /// <returns>Bitmap</returns>
        protected virtual Task SaveToGlobalCache(Uri? url, Stream imageBytes)
        {
            // Current implementation does not provide global caching
            return Task.CompletedTask;
        }

        ~BaseWebImageLoader()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _shouldDisposeHttpClient)
            {
                HttpClient.Dispose();
            }
        }
    }
}

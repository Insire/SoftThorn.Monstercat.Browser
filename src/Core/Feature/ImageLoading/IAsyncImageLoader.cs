using System;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public interface IAsyncImageLoader<T> : IDisposable
        where T : class
    {
        /// <summary>
        /// Loads image
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Bitmap</returns>
        public Task<T?> ProvideImageAsync(Uri? url);
    }
}

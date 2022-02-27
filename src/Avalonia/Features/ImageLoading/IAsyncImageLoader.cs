using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace SoftThorn.Monstercat.Browser.Avalonia
{
    public interface IAsyncImageLoader : IDisposable
    {
        /// <summary>
        /// Loads image
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Bitmap</returns>
        public Task<IBitmap?> ProvideImageAsync(Uri? url);
    }
}

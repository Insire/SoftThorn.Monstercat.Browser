using Avalonia.Media.Imaging;
using SoftThorn.Monstercat.Browser.Core;
using System;
using System.IO;

namespace SoftThorn.Monstercat.Browser.Avalonia
{
    public sealed class ImageFactory : IImageFactory<IBitmap>
    {
        public IBitmap From(Stream stream)
        {
            return new Bitmap(stream);
        }

        public IBitmap From(Uri uri)
        {
            return new Bitmap(uri.ToString());
        }

        public IBitmap From(string uri)
        {
            return new Bitmap(uri);
        }
    }
}

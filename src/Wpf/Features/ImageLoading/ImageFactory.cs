using SoftThorn.Monstercat.Browser.Core;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public sealed class ImageFactory : IImageFactory<BitmapSource>
    {
        public BitmapSource From(Stream stream)
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = stream;
            img.EndInit();

            img.Freeze();

            return img;
        }

        public BitmapSource From(Uri uri)
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.UriSource = uri;
            img.EndInit();

            img.Freeze();

            return img;
        }

        public BitmapSource From(string uri)
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.UriSource = new Uri(uri);
            img.EndInit();

            img.Freeze();

            return img;
        }
    }
}

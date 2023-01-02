using ImageMagick;
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
            var img = Resize(new MagickImage(stream));
            img.Freeze();

            return img;
        }

        public BitmapSource From(Uri uri)
        {
            var img = Resize(new MagickImage(uri.OriginalString));
            img.Freeze();

            return img;
        }

        public BitmapSource From(string uri)
        {
            var img = Resize(new MagickImage(uri));
            img.Freeze();

            return img;
        }

        private static BitmapSource Resize(MagickImage magickImage)
        {
            // Read from file
            using (magickImage)
            {
                var size = new MagickGeometry(300, 300)
                {
                    Less = false,
                    Greater = true,

                    // This will resize the image to a fixed size without maintaining the aspect ratio.
                    // Normally an image will be resized to fit inside the specified size.
                    IgnoreAspectRatio = false
                };

                magickImage.Resize(size);

                // Save the result
                return magickImage.ToBitmapSource();
            }
        }
    }
}

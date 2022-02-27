using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SoftThorn.MonstercatNet;
using System;
using System.Globalization;
using System.IO;

namespace SoftThorn.Monstercat.Browser.Avalonia
{
    public class ReleaseIdToBitmapValueConverter : IValueConverter
    {
        public static ReleaseIdToBitmapValueConverter Instance { get; } = new ReleaseIdToBitmapValueConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string catalogId && targetType == typeof(IImage))
            {
                var cdn = AvaloniaLocator.Current.GetService<IMonstercatCdnService>();

                var requestBuilder = ReleaseCoverArtBuilder
                    .Create(new TrackRelease() { CatalogId = catalogId })
                    .WithMediumCoverArt();

                using var memory = new MemoryStream();
                var stream = cdn.GetReleaseCoverAsStream(requestBuilder).GetAwaiter().GetResult();
                stream.CopyTo(memory);

                memory.Seek(0, SeekOrigin.Begin);

                return new Bitmap(memory);
            }

            throw new NotSupportedException();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

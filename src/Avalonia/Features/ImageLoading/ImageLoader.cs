using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Microsoft.IO;
using SoftThorn.Monstercat.Browser.Core;
using System;
using System.Reactive.Linq;

namespace SoftThorn.Monstercat.Browser.Avalonia
{
    // original idea and code from: https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia
    public static class ImageLoader
    {
        public static RecyclableMemoryStreamManager Manager { get; set; } = new RecyclableMemoryStreamManager();
        public static IImageFactory<IBitmap> ImageFactory { get; set; } = new ImageFactory();
        public static IAsyncImageLoader<IBitmap> AsyncImageLoader { get; set; } = new RamCachedWebImageLoader<IBitmap>(Manager, ImageFactory);

        static ImageLoader()
        {
            SourceProperty.Changed
                .Where(args => args.IsEffectiveValueChange)
                .Subscribe(args => OnSourceChanged((Image)args.Sender, args.NewValue.Value));
        }

        private static async void OnSourceChanged(Image sender, Uri? url)
        {
            SetIsLoading(sender, true);

            var bitmap = await AsyncImageLoader.ProvideImageAsync(url);
            if (GetSource(sender) != url)
            {
                return;
            }

            sender.Source = bitmap;

            SetIsLoading(sender, false);
        }

        public static readonly AttachedProperty<Uri?> SourceProperty = AvaloniaProperty.RegisterAttached<Image, Uri?>("Source", typeof(ImageLoader));

        public static Uri? GetSource(Image element)
        {
            return element.GetValue(SourceProperty);
        }

        public static void SetSource(Image element, Uri? value)
        {
            element.SetValue(SourceProperty, value);
        }

        public static readonly AttachedProperty<bool> IsLoadingProperty = AvaloniaProperty.RegisterAttached<Image, bool>("IsLoading", typeof(ImageLoader));

        public static bool GetIsLoading(Image element)
        {
            return element.GetValue(IsLoadingProperty);
        }

        private static void SetIsLoading(Image element, bool value)
        {
            element.SetValue(IsLoadingProperty, value);
        }
    }
}

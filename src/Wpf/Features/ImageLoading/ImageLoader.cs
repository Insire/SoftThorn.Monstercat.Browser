using Microsoft.IO;
using SoftThorn.Monstercat.Browser.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    // original idea and code from: https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia
    public static class ImageLoader
    {
        public static RecyclableMemoryStreamManager Manager { get; set; } = new RecyclableMemoryStreamManager();
        public static IImageFactory<BitmapSource> ImageFactory { get; set; } = new ImageFactory();
        public static IAsyncImageLoader<BitmapSource> AsyncImageLoader { get; set; } = new RamCachedWebImageLoader<BitmapSource>(Manager, ImageFactory);

        public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached(
            "Source",
            typeof(Uri),
            typeof(ImageLoader),
            new PropertyMetadata(null, OnSourceChanged));

        /// <summary>Helper for getting <see cref="SourceProperty"/> from <paramref name="frameworkElement"/>.</summary>
        /// <param name="frameworkElement"><see cref="FrameworkElement"/> to read <see cref="SourceProperty"/> from.</param>
        /// <returns>First property value.</returns>
        [AttachedPropertyBrowsableForType(typeof(FrameworkElement))]
        public static Uri? GetSource(FrameworkElement frameworkElement)
        {
            return (Uri?)frameworkElement.GetValue(SourceProperty);
        }

        /// <summary>Helper for setting <see cref="FirstProperty"/> on <paramref name="frameworkElement"/>.</summary>
        /// <param name="frameworkElement"><see cref="FrameworkElement"/> to set <see cref="SourceProperty"/> on.</param>
        /// <param name="value">First property value.</param>
        public static void SetSource(FrameworkElement frameworkElement, Uri? value)
        {
            frameworkElement.SetValue(SourceProperty, value);
        }

        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.RegisterAttached(
            "IsLoading",
            typeof(bool),
            typeof(ImageLoader),
            new PropertyMetadata(false));

        /// <summary>Helper for getting <see cref="IsLoadingProperty"/> from <paramref name="frameworkElement"/>.</summary>
        /// <param name="frameworkElement"><see cref="FrameworkElement"/> to read <see cref="IsLoadingProperty"/> from.</param>
        /// <returns>First property value.</returns>
        [AttachedPropertyBrowsableForType(typeof(FrameworkElement))]
        public static bool GetIsLoading(FrameworkElement frameworkElement)
        {
            return (bool)frameworkElement.GetValue(IsLoadingProperty);
        }

        /// <summary>Helper for setting <see cref="IsLoadingProperty"/> on <paramref name="frameworkElement"/>.</summary>
        /// <param name="frameworkElement"><see cref="FrameworkElement"/> to set <see cref="IsLoadingProperty"/> on.</param>
        /// <param name="value">First property value.</param>
        public static void SetIsLoading(FrameworkElement frameworkElement, bool value)
        {
            frameworkElement.SetValue(IsLoadingProperty, value);
        }

        private static void OnSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Image image)
            {
                if (e.NewValue is Uri uri)
                {
                    OnSourceChanged(image, uri);
                }
                else
                {
                    OnSourceChanged(image, null);
                }
            }
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
    }
}

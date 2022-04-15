using DryIoc;
using Gress;
using Jot;
using Jot.Storage;
using Microsoft.Extensions.Configuration;
using SoftThorn.Monstercat.Browser.Core;
using SoftThorn.MonstercatNet;
using System;
using System.Net.Http;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public static class CompositionRoot
    {
        public static IContainer Get()
        {
            var _tracker = new Tracker(new JsonFileStore(Environment.SpecialFolder.CommonApplicationData));
            _tracker.Configure<Shell>()
                .Id(_ => $"[Width={SystemParameters.VirtualScreenWidth},Height{SystemParameters.VirtualScreenHeight}]")
                .Properties(w => new { w.Height, w.Width, w.Left, w.Top, w.WindowState })
                .PersistOn(nameof(Window.Closing))
                .StopTrackingOn(nameof(Window.Closing));

            var playbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };

            var configuration = new ConfigurationBuilder()
                .AddCommandLine(Environment.GetCommandLineArgs())
                .AddEnvironmentVariables()
                .AddUserSecrets<Shell>()
                .Build();

            var _apiHttpClient = new HttpClient().UseMonstercatApiV2();
            var _api = MonstercatApi.Create(_apiHttpClient);

            var container = new Container();

            // views
            container.Register<Shell>(Reuse.Singleton);

            // viewmodels
            container.Register<DownloadViewModel>(Reuse.Singleton);
            container.Register<ShellViewModel>(Reuse.Singleton);
            container.Register<LoginViewModel>(Reuse.Singleton);
            container.Register<ReleasesViewModel>(Reuse.Singleton);
            container.Register<TagsViewModel>(Reuse.Singleton);
            container.Register<BrandsViewModel>(Reuse.Singleton);
            container.Register<GenresViewModel>(Reuse.Singleton);
            container.Register<ArtistsViewModel>(Reuse.Singleton);

            // services
            container.RegisterInstance<IConfiguration>(configuration);
            container.RegisterInstance(_api);
            container.RegisterInstance(_tracker);
            container.RegisterInstance(playbackTimer);
            container.RegisterInstance(SynchronizationContext.Current!);
            container.Register<TrackRepository>(Reuse.Singleton);
            container.Register<DispatcherProgress<Percentage>>(Reuse.Singleton, made: Made.Of(_ => ServiceInfo.Of<DispatcherProgressFactory<Percentage>>(), f => f.Create()));
            container.Register<IPlaybackService, PlaybackService>(Reuse.Singleton);
            container.Register<ProgressContainer<Percentage>>(Reuse.Singleton);
            container.Register<DispatcherProgressFactory<Percentage>>(Reuse.Singleton);

            return container;
        }
    }
}
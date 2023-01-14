using Akavache;
using ATL.Playlist;
using CommunityToolkit.Mvvm.Messaging;
using DryIoc;
using Gress;
using Jot;
using Jot.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.ObjectPool;
using Microsoft.IO;
using Serilog;
using Serilog.Events;
using SoftThorn.Monstercat.Browser.Core;
using SoftThorn.MonstercatNet;
using System;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
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

            container.Use(Assembly.GetAssembly(typeof(CompositionRoot)));

            // views
            container.Register<Shell>(Reuse.Singleton);

            // viewmodels
            container.Register<ShellViewModel>(Reuse.Singleton);
            container.Register<LoginViewModel>(Reuse.Transient);

            container.Register<AboutViewModel>(Reuse.Singleton);
            container.Register<SettingsViewModel>(Reuse.Singleton);
            container.Register<DownloadViewModel>(Reuse.Singleton);
            container.Register<PlaybackViewModel>(Reuse.Singleton);

            container.Register<ReleasesViewModel>(Reuse.Singleton);
            container.Register<TagsViewModel>(Reuse.Singleton);
            container.Register<GenresViewModel>(Reuse.Singleton);
            container.Register<ArtistsViewModel>(Reuse.Singleton);
            container.Register<TracksViewModel>(Reuse.Singleton);

            container.Register<BrandViewModel<Instinct>>(Reuse.Singleton);
            container.Register<BrandViewModel<Uncaged>>(Reuse.Singleton);
            container.Register<BrandViewModel<Silk>>(Reuse.Singleton);

            container.Register<PlaylistIOFactory>(Reuse.Singleton, made: Made.Of(() => PlaylistIOFactory.GetInstance()));
            container.Register<PlaylistFormatsViewModel>(Reuse.Singleton);

            // services
            container.RegisterInstance<IConfiguration>(configuration);
            container.RegisterInstance(_api);
            container.Register<IMonstercatApi, MonstercatApiCache>(setup: Setup.Decorator);
            container.RegisterInstance(_tracker);
            container.RegisterInstance(playbackTimer);
            container.RegisterInstance<IMessenger>(WeakReferenceMessenger.Default);
            container.RegisterInstance<IScheduler>(new SynchronizationContextScheduler(SynchronizationContext.Current!));
            container.Register<MonstercatContentStorageService>(Reuse.Singleton);
            container.Register<IToastService, ToastService>(Reuse.Singleton);
            container.RegisterInstance(new ToastServiceConfiguration());

            container.Register<IFileSystemService, FileSystemService>(Reuse.Singleton);
            container.Register<DownloadService>(Reuse.Singleton);
            container.Register<ITrackRepository, TrackRepository>(Reuse.Singleton);

            container.Register<SearchViewModelFactory>(Reuse.Singleton);
            container.Register<SettingsService>(Reuse.Singleton);
            container.Register<DispatcherProgress<Percentage>>(Reuse.Singleton, made: Made.Of(_ => ServiceInfo.Of<DispatcherProgressFactory<Percentage>>(), f => f.Create()));
            container.Register<IPlaybackService, PlaybackService>(Reuse.Singleton);
            container.Register<ProgressContainer<Percentage>>(Reuse.Singleton);
            container.Register<DispatcherProgressFactory<Percentage>>(Reuse.Singleton);
            container.Register<WindowService>(Reuse.Singleton);
            container.RegisterInstance(BlobCache.Secure);
            container.RegisterInstance(BlobCache.UserAccount);

            // logging
            var log = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithEnvironmentUserName()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            container.RegisterInstance<ILogger>(log);
            container.Register<AtlLogAdapter>(); // logging playlist writing
            Log.Logger = log;

            // caching
            container.RegisterInstance(new RecyclableMemoryStreamManager());
            container.Register<IImageFactory<BitmapSource>, ImageFactory>(Reuse.Singleton);
            container.Register<ObjectPoolProvider, DefaultObjectPoolProvider>(Reuse.Singleton);
            container.Register<StringBuilderPooledObjectPolicy>(Reuse.Singleton);
            container.RegisterDelegate(context =>
            {
                var policy = context.Resolve<StringBuilderPooledObjectPolicy>();
                var provider = context.Resolve<ObjectPoolProvider>();

                return provider.Create(policy);
            });

            container.Validate();

            return container;
        }
    }
}

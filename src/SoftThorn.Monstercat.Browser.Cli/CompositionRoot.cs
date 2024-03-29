using Akavache;
using CommunityToolkit.Mvvm.Messaging;
using DryIoc;
using Gress;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SpectreConsole;
using SoftThorn.Monstercat.Browser.Cli.Commands;
using SoftThorn.Monstercat.Browser.Cli.Services;
using SoftThorn.Monstercat.Browser.Core;
using SoftThorn.MonstercatNet;
using System.Reflection;

namespace SoftThorn.Monstercat.Browser.Cli
{
    public static class CompositionRoot
    {
        public static IContainer Get()
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            var _apiHttpClient = new HttpClient().UseMonstercatApiV2();
            var _api = MonstercatApi.Create(_apiHttpClient);

            var container = new Container();

            container.Use(Assembly.GetAssembly(typeof(CompositionRoot)));

            // cli
            container.Register<DownloadCommand.Settings>(Reuse.Singleton);
            container.Register<DownloadCommand>(Reuse.Singleton);

            // viewmodels
            container.Register<ShellViewModel>(Reuse.Singleton);
            container.Register<LoginViewModel>(Reuse.Transient);
            container.Register<SearchViewModel>(Reuse.Transient);

            container.Register<AboutViewModel>(Reuse.Singleton);
            container.Register<SettingsViewModel>(Reuse.Singleton);
            container.Register<DownloadViewModel>(Reuse.Singleton);
            container.Register<PlaybackViewModel>(Reuse.Singleton);

            container.Register<ReleasesViewModel>(Reuse.Singleton);
            container.Register<TagsViewModel>(Reuse.Singleton);
            container.Register<GenresViewModel>(Reuse.Singleton);
            container.Register<ArtistsViewModel>(Reuse.Singleton);

            container.Register<BrandViewModel<Instinct>>(Reuse.Singleton);
            container.Register<BrandViewModel<Uncaged>>(Reuse.Singleton);
            container.Register<BrandViewModel<Silk>>(Reuse.Singleton);

            // services
            container.RegisterInstance<IConfiguration>(configuration);
            container.RegisterInstance(_api);
            container.RegisterInstance<IMessenger>(WeakReferenceMessenger.Default);
            container.RegisterInstance(new SynchronizationContext());
            container.Register<IPlaybackService, PlaybackService>(Reuse.Singleton);

            //container.Register<ITrackRepository, MockTrackRepository>(Reuse.Singleton);
            container.Register<ITrackRepository, TrackRepository>(Reuse.Singleton);

            container.Register<SearchViewModelFactory>(Reuse.Singleton);
            container.Register<SettingsService>(Reuse.Singleton);
            container.Register<DispatcherProgress<Percentage>>(Reuse.Singleton, made: Made.Of(_ => ServiceInfo.Of<DispatcherProgressFactory<Percentage>>(), f => f.Create()));
            container.Register<ProgressContainer<Percentage>>(Reuse.Singleton);
            container.Register<DispatcherProgressFactory<Percentage>>(Reuse.Singleton);
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
                .WriteTo.SpectreConsole("{Timestamp:HH:mm:ss} [{Level:u4}] {Message:lj}{NewLine}{Exception}", minLevel: LogEventLevel.Information)
                .CreateLogger();

            container.RegisterInstance<ILogger>(log);
            Log.Logger = log;

            return container;
        }
    }
}

using DryIoc;
using Gress;
using Microsoft.Extensions.Configuration;
using SoftThorn.Monstercat.Browser.Core;
using SoftThorn.MonstercatNet;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Avalonia
{
    public static class CompositionRoot
    {
        public static IContainer Get()
        {
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
            container.Register<AboutViewModel>(Reuse.Singleton);
            container.Register<DownloadViewModel>(Reuse.Singleton);
            container.Register<ShellViewModel>(Reuse.Singleton);
            container.Register<LoginViewModel>(Reuse.Singleton);
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

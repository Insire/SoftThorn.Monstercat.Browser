using Akavache;
using CommunityToolkit.Mvvm.Messaging;
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
            container.Register<Shell>(Reuse.Singleton, made: Made.Of(() => new Shell(Arg.Of<ShellViewModel>())));

            // viewmodels
            container.Register<LoginViewModel>(Reuse.Transient);
            container.Register<SearchViewModel>(Reuse.Transient);

            container.Register<AboutViewModel>(Reuse.Singleton);
            container.Register<SettingsViewModel>(Reuse.Singleton);
            container.Register<DownloadViewModel>(Reuse.Singleton);
            container.Register<ShellViewModel>(Reuse.Singleton);
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
            container.RegisterInstance(SynchronizationContext.Current!);
            container.Register<TrackRepository>(Reuse.Singleton);
            container.Register<SearchViewModelFactory>(Reuse.Singleton);
            container.Register<SettingsService>(Reuse.Singleton);
            container.Register<DispatcherProgress<Percentage>>(Reuse.Singleton, made: Made.Of(_ => ServiceInfo.Of<DispatcherProgressFactory<Percentage>>(), f => f.Create()));
            container.Register<IPlaybackService, PlaybackService>(Reuse.Singleton);
            container.Register<ProgressContainer<Percentage>>(Reuse.Singleton);
            container.Register<DispatcherProgressFactory<Percentage>>(Reuse.Singleton);
            //container.Register<WindowService>(Reuse.Singleton);
            container.RegisterInstance(BlobCache.Secure);
            container.RegisterInstance(BlobCache.UserAccount);

            return container;
        }
    }
}

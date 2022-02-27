using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using SoftThorn.Monstercat.Browser.Core;
using SoftThorn.MonstercatNet;
using System;
using System.Net.Http;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Avalonia
{
    public class App : Application
    {
        private readonly ApiCredentials _credentials;
        private readonly PlaybackService _playbackService;

        private readonly HttpClient _apiHttpClient;
        private readonly IMonstercatApi _api;

        private readonly HttpClient _cdnHttpClient;
        private readonly IMonstercatCdnService _cdn;

        private ShellViewModel? _shellViewmodel;

        public App()
        {
            _credentials = new ApiCredentials();
            _apiHttpClient = new HttpClient().UseMonstercatApiV2();
            _api = MonstercatApi.Create(_apiHttpClient);

            _cdnHttpClient = new HttpClient().UseMonstercatCdn();
            _cdn = MonstercatCdn.Create(_cdnHttpClient);

            var configuration = new ConfigurationBuilder()
                .AddCommandLine(Environment.GetCommandLineArgs())
                .AddEnvironmentVariables()
                .AddUserSecrets<Shell>()
                .Build();

            var sectionName = typeof(ApiCredentials).Name;
            var section = configuration.GetSection(sectionName);

            section.Bind(_credentials);

            _playbackService = new PlaybackService();

            AvaloniaLocator.CurrentMutable.Bind<PlaybackService>().ToFunc(() => _playbackService);
            AvaloniaLocator.CurrentMutable.Bind<IMonstercatCdnService>().ToFunc(() => _cdn);
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var shellViewModel = _shellViewmodel = new ShellViewModel(SynchronizationContext.Current!, _api, _playbackService);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new Shell(shellViewModel);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}

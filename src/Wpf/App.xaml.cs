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
    public partial class App : Application
    {
        private readonly ApiCredentials _credentials;
        private readonly HttpClient _apiHttpClient;
        private readonly IMonstercatApi _api;
        private readonly PlaybackService _playbackService;

        private Tracker? _tracker;
        private ShellViewModel? _shellViewmodel;

        public App()
        {
            _credentials = new ApiCredentials();
            _apiHttpClient = new HttpClient().UseMonstercatApiV2();
            _api = MonstercatApi.Create(_apiHttpClient);

            var configuration = new ConfigurationBuilder()
                .AddCommandLine(Environment.GetCommandLineArgs())
                .AddEnvironmentVariables()
                .AddUserSecrets<Shell>()
                .Build();

            var sectionName = typeof(ApiCredentials).Name;
            var section = configuration.GetSection(sectionName);

            section.Bind(_credentials);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };

            _playbackService = new PlaybackService(_api, timer);
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            await _api.Login(_credentials);

            _tracker = new Tracker(new JsonFileStore(Environment.SpecialFolder.CommonApplicationData));
            _tracker.Configure<Shell>()
                .Id(_ => $"[Width={SystemParameters.VirtualScreenWidth},Height{SystemParameters.VirtualScreenHeight}]")
                .Properties(w => new { w.Height, w.Width, w.Left, w.Top, w.WindowState })
                .PersistOn(nameof(Window.Closing))
                .StopTrackingOn(nameof(Window.Closing));

            var shellViewModel = _shellViewmodel = new ShellViewModel(SynchronizationContext.Current!, _api, _playbackService);
            var shell = new Shell(shellViewModel, _tracker);

            shell.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _tracker?.PersistAll();

            _shellViewmodel?.Dispose();

            base.OnExit(e);
        }
    }
}

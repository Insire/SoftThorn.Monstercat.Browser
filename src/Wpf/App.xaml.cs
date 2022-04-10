using Gress;
using Jot;
using Jot.Storage;
using Microsoft.Extensions.Configuration;
using SoftThorn.Monstercat.Browser.Core;
using SoftThorn.MonstercatNet;
using System;
using System.Net.Http;
using System.Reactive.Disposables;
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

        private CompositeDisposable? _subscription;

        private Tracker? _tracker;

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

            var synchronizationContext = SynchronizationContext.Current!;
            var progress = new ProgressContainer<Percentage>();
            var dispatcherProgress = new DispatcherProgress<Percentage>(synchronizationContext, (p) => progress.Report(p), TimeSpan.FromMilliseconds(250));
            var trackRepository = new TrackRepository(dispatcherProgress, _api);
            var downloadViewModel = new DownloadViewModel(SynchronizationContext.Current!, _api, trackRepository);
            var shellViewModel = new ShellViewModel(synchronizationContext, trackRepository, _playbackService, downloadViewModel, progress);
            var shell = new Shell(shellViewModel, downloadViewModel, _tracker);

            _subscription = new CompositeDisposable(dispatcherProgress, trackRepository, downloadViewModel, shellViewModel);

            shell.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _tracker?.PersistAll();

            _subscription?.Dispose();

            base.OnExit(e);
        }
    }
}

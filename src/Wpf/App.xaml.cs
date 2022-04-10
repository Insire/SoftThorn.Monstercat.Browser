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
        private readonly HttpClient _apiHttpClient;
        private readonly PlaybackService _playbackService;
        private readonly IMonstercatApi _api;
        private readonly IConfiguration _configuration;

        private CompositeDisposable? _subscription;

        private Tracker? _tracker;

        public App()
        {
            _apiHttpClient = new HttpClient().UseMonstercatApiV2();
            _api = MonstercatApi.Create(_apiHttpClient);

            _configuration = new ConfigurationBuilder()
                .AddCommandLine(Environment.GetCommandLineArgs())
                .AddEnvironmentVariables()
                .AddUserSecrets<Shell>()
                .Build();

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };

            _playbackService = new PlaybackService(_api, timer);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _tracker = new Tracker(new JsonFileStore(Environment.SpecialFolder.CommonApplicationData));
            _tracker.Configure<Shell>()
                .Id(_ => $"[Width={SystemParameters.VirtualScreenWidth},Height{SystemParameters.VirtualScreenHeight}]")
                .Properties(w => new { w.Height, w.Width, w.Left, w.Top, w.WindowState })
                .PersistOn(nameof(Window.Closing))
                .StopTrackingOn(nameof(Window.Closing));

            var loginViewModel = new LoginViewModel(_api, _configuration);
            var synchronizationContext = SynchronizationContext.Current!;
            var progress = new ProgressContainer<Percentage>();
            var dispatcherProgress = new DispatcherProgress<Percentage>(synchronizationContext, (p) => progress.Report(p), TimeSpan.FromMilliseconds(250));
            var trackRepository = new TrackRepository(dispatcherProgress, _api);
            var downloadViewModel = new DownloadViewModel(SynchronizationContext.Current!, _api, trackRepository);
            var shellViewModel = new ShellViewModel(synchronizationContext, trackRepository, _playbackService, downloadViewModel, loginViewModel, progress);
            var shell = new Shell(shellViewModel, _tracker);

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

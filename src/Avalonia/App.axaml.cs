using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Gress;
using Microsoft.Extensions.Configuration;
using SoftThorn.Monstercat.Browser.Core;
using SoftThorn.MonstercatNet;
using System;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Avalonia
{
    public class App : Application
    {
        private readonly HttpClient _apiHttpClient;
        private readonly IMonstercatApi _api;

        private readonly HttpClient _cdnHttpClient;
        private readonly IMonstercatCdnService _cdn;

        private readonly PlaybackService _playbackService;
        private readonly IConfiguration _configuration;

        private CompositeDisposable? _subscription;

        public App()
        {
            _apiHttpClient = new HttpClient().UseMonstercatApiV2();
            _api = MonstercatApi.Create(_apiHttpClient);

            _cdnHttpClient = new HttpClient().UseMonstercatCdn();
            _cdn = MonstercatCdn.Create(_cdnHttpClient);

            _configuration = new ConfigurationBuilder()
                .AddCommandLine(Environment.GetCommandLineArgs())
                .AddEnvironmentVariables()
                .AddUserSecrets<Shell>()
                .Build();

            _playbackService = new PlaybackService();
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var loginViewModel = new LoginViewModel(_api, _configuration);
            var synchronizationContext = SynchronizationContext.Current!;
            var progress = new ProgressContainer<Percentage>();
            var dispatcherProgress = new DispatcherProgress<Percentage>(synchronizationContext, (p) => progress.Report(p), TimeSpan.FromMilliseconds(250));
            var trackRepository = new TrackRepository(dispatcherProgress, _api);
            var downloadViewModel = new DownloadViewModel(SynchronizationContext.Current!, _api, trackRepository);
            var shellViewModel = new ShellViewModel(synchronizationContext, trackRepository, _playbackService, downloadViewModel, loginViewModel, progress);

            _subscription = new CompositeDisposable(dispatcherProgress, trackRepository, downloadViewModel, shellViewModel);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new Shell(shellViewModel);
            }

            base.OnFrameworkInitializationCompleted();
        }

        // TODO dispose _subscription
    }
}

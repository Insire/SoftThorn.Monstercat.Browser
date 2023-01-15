using DryIoc;
using Jot;
using Microsoft.IO;
using SoftThorn.Monstercat.Browser.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public partial class App : Application
    {
        private IContainer? _container;

        public App()
        {
            Akavache.Registrations.Start("SoftThorn.Monstercat.Browser.Wpf");
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // exception handling
            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += OnUnhandledException;

            App.Current.Dispatcher.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // startup
            var container = _container = CompositionRoot.Get();

            ImageLoader.Manager = container.Resolve<RecyclableMemoryStreamManager>();
            ImageLoader.ImageFactory = container.Resolve<IImageFactory<BitmapSource>>();

            var settingsViewModel = container.Resolve<SettingsViewModel>();
            // the shellviewmodel has to exist, so that loading the settings updates its own and all of its dependencies initial state
            var shellViewModel = container.Resolve<ShellViewModel>();

            await settingsViewModel.Load();

            var shell = container.Resolve<Shell>();
            shell.Closing += OnShellClosing;
            shell.Show();

            var loginViewModel = container.Resolve<LoginViewModel>();
            await loginViewModel.TryLogin(null, CancellationToken.None);

            await shellViewModel.Refresh();
        }

        private async void OnShellClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var settings = _container?.Resolve<SettingsService>();
            if (settings == null)
            {
                return;
            }

            await settings.Save();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            var tracker = _container?.Resolve<Tracker>();
            tracker?.PersistAll();

            Akavache.BlobCache.Shutdown().Wait();

            Serilog.Log.CloseAndFlush();

            _container?.Dispose();

            base.OnExit(e);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                Serilog.Log.Error(exception, "AppDomain.CurrentDomain.UnhandledException");
            }
        }

        private static void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Serilog.Log.Error(e.Exception, "App.Current.Dispatcher.UnhandledException");
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Serilog.Log.Error(e.Exception, "TaskScheduler.UnobservedTaskException");
        }
    }
}

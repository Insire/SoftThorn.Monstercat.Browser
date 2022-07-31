using DryIoc;
using Jot;
using SoftThorn.Monstercat.Browser.Core;
using System.Threading;
using System.Windows;

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

            var container = _container = CompositionRoot.Get();

            var settingsViewModel = container.Resolve<SettingsViewModel>();
            // the shellviewmodel has to exist, so that loading the settings updates its own and all of its dependencies initial state
            var shellViewModel = container.Resolve<ShellViewModel>();

            await settingsViewModel.Load();

            var shell = container.Resolve<Shell>();
            shell.Show();

            var loginViewModel = container.Resolve<LoginViewModel>();
            await loginViewModel.TryLogin(null, CancellationToken.None);

            await shellViewModel.Refresh();
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
    }
}

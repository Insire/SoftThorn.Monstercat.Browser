using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DryIoc;
using SoftThorn.Monstercat.Browser.Core;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Avalonia
{
    public class App : Application
    {
        private IContainer? _container;

        public App()
        {
            Akavache.Registrations.Start("SoftThorn.Monstercat.Browser.Avalonia");
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            var container = _container = CompositionRoot.Get();

            var settingsViewModel = container.Resolve<SettingsViewModel>();
            // the shellviewmodel has to exist, so that loading the settings updates its own and all of its dependencies initial state
            var shellViewModel = container.Resolve<ShellViewModel>();

            await settingsViewModel.Load();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Exit += Desktop_Exit;
                desktop.MainWindow = new Shell(shellViewModel);

                var loginViewModel = container.Resolve<LoginViewModel>();
                await loginViewModel.TryLogin(null, CancellationToken.None);

                await shellViewModel.Refresh();

                desktop.MainWindow.Show();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            Akavache.BlobCache.Shutdown().Wait();

            _container?.Dispose();
        }
    }
}

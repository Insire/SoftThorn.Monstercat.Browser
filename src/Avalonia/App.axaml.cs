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

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            var container = _container = CompositionRoot.Get();

            var shell = container.Resolve<Shell>();
            var shellViewModel = container.Resolve<ShellViewModel>();
            shell.Show();

            await shellViewModel.TryLogin(null, CancellationToken.None);
            await shellViewModel.Refresh();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Exit += Desktop_Exit;
                desktop.MainWindow = new Shell(shellViewModel);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            _container?.Dispose();
        }
    }
}

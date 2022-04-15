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

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var container = _container = CompositionRoot.Get();

            var shell = container.Resolve<Shell>();
            var shellViewModel = container.Resolve<ShellViewModel>();
            shell.Show();

            await shellViewModel.TryLogin(null, CancellationToken.None);
            await shellViewModel.Refresh();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            var tracker = _container?.Resolve<Tracker>();
            tracker?.PersistAll();

            _container?.Dispose();

            base.OnExit(e);
        }
    }
}

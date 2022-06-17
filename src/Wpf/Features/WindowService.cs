using Ookii.Dialogs.Wpf;
using SoftThorn.Monstercat.Browser.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public sealed class WindowService
    {
        private static (bool, string folder) TrySelectFolder()
        {
            var dlg = new VistaFolderBrowserDialog
            {
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true,
                Description = "Select a folder"
            };

            var result = dlg.ShowDialog();

            return (result ?? false, dlg.SelectedPath);
        }

        private readonly ShellViewModel _shellViewModel;
        private readonly Func<LoginViewModel> _loginViewModelFactory;
        private readonly SearchViewModelFactory _searchViewModelFactory;

        public WindowService(ShellViewModel shellViewModel, Func<LoginViewModel> loginViewModelFactory, SearchViewModelFactory searchViewModelFactory)
        {
            _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));
            _searchViewModelFactory = searchViewModelFactory ?? throw new ArgumentNullException(nameof(searchViewModelFactory));
            _loginViewModelFactory = loginViewModelFactory ?? throw new ArgumentNullException(nameof(loginViewModelFactory));
        }

        public void ShowLoginDialog(Window owner)
        {
            var login = _loginViewModelFactory();
            var loginView = new LoginView(login)
            {
                Owner = owner,
            };
            login.ClearValidation();
            login.OnLogin = () => loginView.DialogResult = true;

            loginView.ShowDialog();
        }

        public async Task ShowSearchView(Window owner)
        {
            var wasLoggedIn = _shellViewModel.IsLoggedIn;

            if (!_shellViewModel.IsLoggedIn)
            {
                var login = _loginViewModelFactory();
                await login.TryLogin(() => ShowLoginDialog(owner), CancellationToken.None);
            }

            if (wasLoggedIn != _shellViewModel.IsLoggedIn)
            {
                await _shellViewModel.Refresh();
            }

            var search = await _searchViewModelFactory.Create();
            var wnd = new SearchView(search)
            {
                Owner = owner,
            };
            search.OnDownloadStarted = () => wnd.DialogResult = true;

            wnd.ShowDialog();

            search.OnDownloadStarted = null;
        }

        public void ShowAbout(Window owner)
        {
            var wnd = new AboutView(_shellViewModel.About)
            {
                Owner = owner,
            };

            wnd.ShowDialog();
        }

        public void ShowSettings(Window owner)
        {
            _shellViewModel.Settings.SelectFolderProxy = TrySelectFolder;
            var settingsView = new SettingsView(_shellViewModel.Settings)
            {
                Owner = owner,
            };
            _shellViewModel.Settings.OnSuccssfulSave += OnSuccssfulSave;

            settingsView.ShowDialog();
            _shellViewModel.Settings.SelectFolderProxy = null;

            void OnSuccssfulSave()
            {
                _shellViewModel.Settings.OnSuccssfulSave -= OnSuccssfulSave;
                settingsView.DialogResult = true;
            }
        }
    }
}

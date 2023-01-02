using Ookii.Dialogs.Wpf;
using SoftThorn.Monstercat.Browser.Core;
using System;
using System.Reactive.Linq;
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
        private readonly SynchronizationContext _synchronizationContext;
        private readonly ITrackRepository _trackRepository;

        public WindowService(
            ShellViewModel shellViewModel,
            Func<LoginViewModel> loginViewModelFactory,
            SearchViewModelFactory searchViewModelFactory,
            SynchronizationContext synchronizationContext,
            ITrackRepository trackRepository)
        {
            _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));
            _searchViewModelFactory = searchViewModelFactory ?? throw new ArgumentNullException(nameof(searchViewModelFactory));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _trackRepository = trackRepository ?? throw new ArgumentNullException(nameof(trackRepository));
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

        public async Task ShowSearchView<TBrand>(
            Window owner,
            BrandViewModel<TBrand>? brand = null,
            ReleaseViewModel? release = null,
            ArtistViewModel? artist = null,
            TrackViewModel? track = null)
            where TBrand : Brand, new()
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

            using var search = track is null
                ? brand is null
                ? release is null
                ? artist is null
                ? await _searchViewModelFactory.Create()
                : await _searchViewModelFactory.CreateFromArtist(artist)
                : await _searchViewModelFactory.CreateFromRelease(release)
                : await _searchViewModelFactory.CreateFromBrand(brand)
                : await _searchViewModelFactory.CreateFromTrack(track);

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
            var wnd = new SettingsView(_shellViewModel.Settings)
            {
                Owner = owner,
            };
            _shellViewModel.Settings.OnSuccssfulSave = OnSuccssfulSave;

            wnd.ShowDialog();
            _shellViewModel.Settings.SelectFolderProxy = null;

            void OnSuccssfulSave()
            {
                _shellViewModel.Settings.OnSuccssfulSave = null;
                wnd.DialogResult = true;
            }
        }

        public void ShowData(Window owner)
        {
            var wnd = new DataView(new DataViewModel(_synchronizationContext, _trackRepository))
            {
                Owner = owner,
            };

            wnd.ShowDialog();
        }
    }
}

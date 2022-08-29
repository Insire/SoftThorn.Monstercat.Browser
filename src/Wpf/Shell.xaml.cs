using CommunityToolkit.Mvvm.Input;
using Jot;
using SoftThorn.Monstercat.Browser.Core;
using System;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public partial class Shell
    {
        private readonly WindowService _windowService;
        private readonly ShellViewModel _shellViewModel;

        public Shell(WindowService windowService, ShellViewModel shellViewModel, Tracker tracker)
        {
            if (tracker is null)
            {
                throw new ArgumentNullException(nameof(tracker));
            }

            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            DataContext = _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));

            InitializeComponent();

            tracker.Track(this);
        }

        [RelayCommand]
        private async Task DownloadAll()
        {
            await _windowService.ShowSearchView<Silk>(this, null);
        }

        [RelayCommand]
        private async Task DownloadRelease(ReleaseViewModel? release)
        {
            await _windowService.ShowSearchView<Silk>(this, release: release);
        }

        [RelayCommand]
        private async Task DownloadArtist(ArtistViewModel? artist)
        {
            await _windowService.ShowSearchView<Silk>(this, artist: artist);
        }

        [RelayCommand]
        private void About()
        {
            _windowService.ShowAbout(this);
        }

        [RelayCommand]
        private void Settings()
        {
            _windowService.ShowSettings(this);
        }

        [RelayCommand]
        private async Task DownloadSilk()
        {
            await _windowService.ShowSearchView(this, brand: _shellViewModel.Silk);
        }

        [RelayCommand]
        private async Task DownloadInstinct()
        {
            await _windowService.ShowSearchView(this, brand: _shellViewModel.Instinct);
        }

        [RelayCommand]
        private async Task DownloadUncaged()
        {
            await _windowService.ShowSearchView(this, brand: _shellViewModel.Uncaged);
        }

        private void Playlist_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PlaylistPopup.IsPopupOpen = !PlaylistPopup.IsPopupOpen;
        }
    }
}

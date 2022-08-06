using Jot;
using SoftThorn.Monstercat.Browser.Core;
using System;

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

        private async void Download_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await _windowService.ShowSearchView<Silk>(this, null);
        }

        private void About_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _windowService.ShowAbout(this);
        }

        private void Settings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _windowService.ShowSettings(this);
        }

        private void Playlist_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PlaylistPopup.IsPopupOpen = !PlaylistPopup.IsPopupOpen;
        }

        private async void DownloadSilk_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await _windowService.ShowSearchView(this, _shellViewModel.Silk);
        }

        private async void DownloadInstinct_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await _windowService.ShowSearchView(this, _shellViewModel.Instinct);
        }

        private async void DownloadUncaged_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await _windowService.ShowSearchView(this, _shellViewModel.Uncaged);
        }
    }
}

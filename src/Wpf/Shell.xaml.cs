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
            await _windowService.ShowSearchView(this);
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
            Playlist.IsOpen = !Playlist.IsOpen;
        }
    }
}

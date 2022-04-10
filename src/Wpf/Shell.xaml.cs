using Jot;
using Ookii.Dialogs.Wpf;
using SoftThorn.Monstercat.Browser.Core;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public partial class Shell
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

        public Shell(ShellViewModel shellViewModel, Tracker tracker)
        {
            DataContext = _shellViewModel = shellViewModel;

            InitializeComponent();

            tracker.Track(this);
        }

        private async void Download_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!_shellViewModel.Login.IsLoggedIn)
            {
                _shellViewModel.Login.Validate();
                if (_shellViewModel.Login.HasErrors)
                {
                    var loginView = new LoginView(_shellViewModel.Login)
                    {
                        Owner = this,
                    };
                    _shellViewModel.Login.ClearValidation();
                    _shellViewModel.Login.OnLogin = () => loginView.DialogResult = true;

                    loginView.ShowDialog();
                }
                else
                {
                    await _shellViewModel.Login.Login(CancellationToken.None);
                }
            }

            if (!_shellViewModel.Login.IsLoggedIn)
            {
                return;
            }

            var wnd = new DownloadView(_shellViewModel.Downloads)
            {
                Owner = this,
            };
            _shellViewModel.Downloads.SelectFolderProxy = TrySelectFolder;
            _shellViewModel.Downloads.OnDownloadStarted = () => wnd.DialogResult = true;

            wnd.ShowDialog();

            _shellViewModel.Downloads.OnDownloadStarted = null;
            _shellViewModel.Downloads.SelectFolderProxy = null;
        }
    }
}

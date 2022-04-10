using Jot;
using Ookii.Dialogs.Wpf;
using SoftThorn.Monstercat.Browser.Core;

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

        private readonly DownloadViewModel _downloadViewModel;

        public Shell(ShellViewModel shellViewModel, DownloadViewModel downloadViewModel, Tracker tracker)
        {
            DataContext = shellViewModel;
            _downloadViewModel = downloadViewModel;

            InitializeComponent();

            tracker.Track(this);
        }

        private void Download_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var wnd = new DownloadView(_downloadViewModel)
            {
                Owner = this,
            };
            _downloadViewModel.SelectFolderProxy = TrySelectFolder;
            _downloadViewModel.OnDownloadStarted = () => wnd.DialogResult = true;

            wnd.ShowDialog();

            _downloadViewModel.OnDownloadStarted = null;
            _downloadViewModel.SelectFolderProxy = null;
        }
    }
}

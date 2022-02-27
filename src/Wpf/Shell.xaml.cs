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

        private readonly ShellViewModel _shellViewModel;

        public Shell(ShellViewModel shellViewModel, Tracker tracker)
        {
            DataContext = _shellViewModel = shellViewModel;

            shellViewModel.SelectFolderProxy = TrySelectFolder;

            InitializeComponent();

            tracker.Track(this);
        }
    }
}

using SoftThorn.Monstercat.Browser.Core;
using System;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public partial class DownloadView
    {
        public DownloadView(DownloadViewModel downloadViewModel)
        {
            if (downloadViewModel is null)
            {
                throw new ArgumentNullException(nameof(downloadViewModel));
            }

            DataContext = downloadViewModel;
            InitializeComponent();
        }

        private void OnCancelClick(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

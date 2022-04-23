using SoftThorn.Monstercat.Browser.Core;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public partial class AboutView
    {
        public AboutView(AboutViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}

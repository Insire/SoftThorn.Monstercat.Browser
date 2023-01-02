using SoftThorn.Monstercat.Browser.Core;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public partial class DataView
    {
        public DataView()
        {
            InitializeComponent();
        }

        public DataView(DataViewModel dataViewModel)
        {
            DataContext = dataViewModel;
            InitializeComponent();
        }
    }
}

using SoftThorn.Monstercat.Browser.Core;
using System;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public partial class SearchView
    {
        public SearchView(SearchViewModel searchViewModel)
        {
            if (searchViewModel is null)
            {
                throw new ArgumentNullException(nameof(searchViewModel));
            }

            DataContext = searchViewModel;
            InitializeComponent();
        }
    }
}

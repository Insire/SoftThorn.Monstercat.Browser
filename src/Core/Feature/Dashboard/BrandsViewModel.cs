using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using System.Collections.ObjectModel;

namespace SoftThorn.Monstercat.Browser.Core
{
    /// <summary>
    /// top 15 tracks by brand, sorted by newest
    /// </summary>
    [ObservableObject]
    public sealed partial class BrandsViewModel : IDashBoardEntryViewModel
    {
        public ObservableCollection<ISeries> SeriesCollection { get; }
    }
}

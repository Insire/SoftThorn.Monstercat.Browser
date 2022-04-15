using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using System.Collections.ObjectModel;

namespace SoftThorn.Monstercat.Browser.Core
{
    /// <summary>
    /// tracks grouped by genre - count Histogram
    /// </summary>
    [ObservableObject]
    public sealed partial class GenresViewModel : IDashBoardEntryViewModel
    {
        public ObservableCollection<ISeries> SeriesCollection { get; }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using System.Collections.ObjectModel;

namespace SoftThorn.Monstercat.Browser.Core
{
    /// <summary>
    /// tracks grouped by genre - count Histogram
    /// </summary>
    public sealed class GenresViewModel : ObservableObject
    {
        public ObservableCollection<ISeries> SeriesCollection { get; }
    }
}

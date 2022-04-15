using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using System.Collections.ObjectModel;

namespace SoftThorn.Monstercat.Browser.Core
{
    /// <summary>
    /// top 15 artists, sorted by newest release
    /// </summary>
    [ObservableObject]
    public sealed partial class ArtistsViewModel : IDashBoardEntryViewModel
    {
        public ObservableCollection<ISeries> SeriesCollection { get; }
    }
}

using LiveChartsCore;
using System.Collections.ObjectModel;

namespace SoftThorn.Monstercat.Browser.Core
{
    public interface IDashBoardEntryViewModel
    {
        ObservableCollection<ISeries> SeriesCollection { get; }
    }
}

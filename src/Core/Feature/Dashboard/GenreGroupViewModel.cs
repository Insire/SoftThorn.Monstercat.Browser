using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class GenreGroupViewModel : ObservableObject
    {
        private readonly IReadOnlyCollection<TrackViewModel> _tracks;

        [ObservableProperty]
        private string? _name;

        [ObservableProperty]
        private int _count;

        public GenreGroupViewModel(IReadOnlyCollection<TrackViewModel> tracks)
        {
            _tracks = tracks;
        }

        public IReadOnlyCollection<TrackViewModel> GetTracks()
        {
            return _tracks;
        }
    }
}

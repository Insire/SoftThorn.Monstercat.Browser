using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class TagGroupViewModel : ObservableObject
    {
        private readonly IReadOnlyCollection<TrackViewModel> _tracks;

        [ObservableProperty]
        private string? _name;

        [ObservableProperty]
        private int _count;

        public TagGroupViewModel(IReadOnlyCollection<TrackViewModel> tracks)
        {
            _tracks = tracks;
        }

        public IReadOnlyCollection<TrackViewModel> GetTracks()
        {
            return _tracks;
        }
    }
}

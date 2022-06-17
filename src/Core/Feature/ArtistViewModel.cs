using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class ArtistViewModel : ObservableObject
    {
        [ObservableProperty]
        private Guid _id;

        [ObservableProperty]
        private string _name = default!;

        [ObservableProperty]
        private DateTime _latestReleaseDate;

        [ObservableProperty]
        private string _uri = default!;

        public ObservableCollection<TrackViewModel> Tracks { get; init; } = default!;
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class ReleaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _artistsTitle = default!;

        [ObservableProperty]
        private string _catalogId = default!;

        [ObservableProperty]
        private Guid _id;

        [ObservableProperty]
        private string[]? _tags;

        [ObservableProperty]
        private string _title = default!;

        [ObservableProperty]
        private string _type = default!;

        [ObservableProperty]
        private DateTime _releaseDate;

        [ObservableProperty]
        private string _version = default!;

        [ObservableProperty]
        private string _upc = default!;

        [ObservableProperty]
        private string _description = default!;

        public Uri? ImageUrl { get; init; }

        public ObservableCollection<TrackViewModel> Tracks { get; init; } = default!;
    }
}

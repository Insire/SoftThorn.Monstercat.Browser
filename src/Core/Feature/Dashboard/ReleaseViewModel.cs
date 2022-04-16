using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class ReleaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _artistsTitle;

        [ObservableProperty]
        private string _catalogId;

        [ObservableProperty]
        private Guid _id;

        [ObservableProperty]
        private string[]? _tags;

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _type;

        [ObservableProperty]
        private DateTime _releaseDate;

        [ObservableProperty]
        private string _version;

        [ObservableProperty]
        private string _upc;

        [ObservableProperty]
        private string _description;

        public Uri? ImageUrl { get; init; }

        public ObservableCollection<TrackViewModel> Tracks { get; init; }
    }
}

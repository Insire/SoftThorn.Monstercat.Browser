using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class TrackViewModel : ObservableObject
    {
        [ObservableProperty]
        private Guid _id;

        [ObservableProperty]
        private Guid _releaseId;

        [ObservableProperty]
        private string _catalogId = default!;

        [ObservableProperty]
        private string _artistsTitle = default!;

        [ObservableProperty]
        private DateTime _debutDate;

        [ObservableProperty]
        private DateTime _releaseDate;

        [ObservableProperty]
        private bool _downloadable;

        [ObservableProperty]
        private bool _inEarlyAccess;

        [ObservableProperty]
        private bool _streamable;

        [ObservableProperty]
        private string _title = default!;

        [ObservableProperty]
        private string _type = default!;

        [ObservableProperty]
        private string _version = default!;

        [ObservableProperty]
        private string _brand = default!;

        [ObservableProperty]
        private string _genrePrimary = default!;

        [ObservableProperty]
        private string _genreSecondary = default!;

        [ObservableProperty]
        private ObservableCollection<string> _tags = default!;

        public Uri? ImageUrl { get; init; }
    }
}

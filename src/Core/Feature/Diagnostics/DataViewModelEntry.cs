using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class DataViewModelEntry : ObservableObject
    {
        [ObservableProperty]
        private string _key = default!;

        [ObservableProperty]
        private Guid _id;

        [ObservableProperty]
        private string _catalogId = default!;

        [ObservableProperty]
        private string _artistsTitle = default!;

        [ObservableProperty]
        private DateTime? _debutDate;

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
        private string _releaseArtistTitle = default!;

        [ObservableProperty]
        private string _releaseCatalogId = default!;

        [ObservableProperty]
        private Guid _releaseId;

        [ObservableProperty]
        private string _releaseTitle = default!;

        [ObservableProperty]
        private string _releaseType = default!;

        [ObservableProperty]
        private DateTime _releaseDate;

        [ObservableProperty]
        private string _releaseVersion = default!;

        [ObservableProperty]
        private string _upc = default!;

        [ObservableProperty]
        private string _description = default!;
    }
}

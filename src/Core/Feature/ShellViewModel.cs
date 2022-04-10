using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Gress;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    [ObservableObject]
    public sealed partial class ShellViewModel : IDisposable
    {
        private readonly ObservableCollectionExtended<TrackViewModel> _tracks;
        private readonly IDisposable _subscription;
        private readonly TrackRepository _trackRepository;
        private readonly IPlaybackService _playbackService;
        private readonly CancellationTokenSource _cancellationTokenSource;

        [ObservableProperty]
        private string? _textFilter;

        [ObservableProperty]
        private bool _isLoading;

        private bool _disposedValue;

        public IAsyncRelayCommand PlayCommand { get; }

        public IAsyncRelayCommand RefreshCommand { get; }

        public ReadOnlyObservableCollection<TrackViewModel> Tracks { get; }

        public ProgressContainer<Percentage> Progress { get; }

        public DownloadViewModel Downloads { get; }

        public ShellViewModel(SynchronizationContext synchronizationContext,
                              TrackRepository trackRepository,
                              IPlaybackService playbackService,
                              DownloadViewModel downloadViewModel,
                              ProgressContainer<Percentage> progress)
        {
            if (synchronizationContext is null)
            {
                throw new ArgumentNullException(nameof(synchronizationContext));
            }

            _trackRepository = trackRepository ?? throw new ArgumentNullException(nameof(trackRepository));
            _playbackService = playbackService ?? throw new ArgumentNullException(nameof(playbackService));
            Downloads = downloadViewModel ?? throw new ArgumentNullException(nameof(downloadViewModel));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));

            _cancellationTokenSource = new CancellationTokenSource();
            _tracks = new ObservableCollectionExtended<TrackViewModel>();

            Tracks = new ReadOnlyObservableCollection<TrackViewModel>(_tracks);
            RefreshCommand = new AsyncRelayCommand(Refresh);
            PlayCommand = new AsyncRelayCommand<object?>(Play, CanPlay);

            var filter = this.WhenPropertyChanged(x => x.TextFilter)
                .DistinctUntilChanged()
                .Throttle(TimeSpan.FromMilliseconds(250))
                .Select(x => BuildFilter(x.Value));

            _subscription = trackRepository
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .Filter(filter)
                .Sort(SortExpressionComparer<KeyValuePair<string, Track>>
                    .Descending(p => p.Value.Release.ReleaseDate.Date)
                    .ThenByAscending(p => p.Value.Title))
                .Transform(p => new TrackViewModel()
                {
                    Id = p.Value.Id,
                    CatalogId = p.Value.Release.CatalogId,
                    ReleaseId = p.Value.Release.Id,
                    DebutDate = p.Value.DebutDate,
                    ReleaseDate = p.Value.Release.ReleaseDate,
                    InEarlyAccess = p.Value.InEarlyAccess,
                    Downloadable = p.Value.Downloadable,
                    Streamable = p.Value.Streamable,
                    Title = p.Value.Title,
                    ArtistsTitle = p.Value.ArtistsTitle,
                    GenreSecondary = p.Value.GenreSecondary,
                    GenrePrimary = p.Value.GenrePrimary,
                    Brand = p.Value.Brand,
                    Version = p.Value.Version,
                    Tags = p.Value.CreateTags(),
                    ImageUrl = p.Value.Release.GetSmallCoverArtUri(),
                })
                .ObserveOn(synchronizationContext)
                .Bind(_tracks)
                .Subscribe();

            static Func<KeyValuePair<string, Track>, bool> BuildFilter(string? searchText)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    return _ => true;
                }

                return vm => vm.Value.Title.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) > -1
                    || vm.Value.ArtistsTitle.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) > -1;
            }
        }

        private async Task Play(object? args)
        {
            if (args is TrackViewModel track)
            {
                var request = new TrackStreamRequest()
                {
                    TrackId = track.Id,
                    ReleaseId = track.ReleaseId,
                };

                await _playbackService.Play(request);
            }
        }

        private bool CanPlay(object? args)
        {
            return args is TrackViewModel;
        }

        private async Task Refresh()
        {
            IsLoading = true;
            try
            {
                await _trackRepository.Refresh();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _subscription.Dispose();
                    _cancellationTokenSource.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

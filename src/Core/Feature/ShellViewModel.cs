using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Gress;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    [ObservableObject]
    public sealed partial class ShellViewModel : IDisposable
    {
        private readonly SourceCache<KeyValuePair<string, Track>, string> _cache;
        private readonly ObservableCollectionExtended<TrackViewModel> _releases;
        private readonly IMonstercatApi _api;
        private readonly IDisposable _subscription;
        private readonly IPlaybackService _playbackService;
        private readonly DispatcherProgress<Percentage> _progressService;
        private readonly CancellationTokenSource _cancellationTokenSource;

        [ObservableProperty]
        private string? _textFilter;

        [AlsoNotifyCanExecuteFor(nameof(DownloadCommand))]
        [ObservableProperty]
        private string? _downloadPath;

        [AlsoNotifyCanExecuteFor(nameof(DownloadCommand))]
        [ObservableProperty]
        private bool _isLoading;

        [AlsoNotifyCanExecuteFor(nameof(DownloadCommand))]
        [ObservableProperty]
        private bool _isDownLoading;

        [ObservableProperty]
        private bool _isDialogOpen;

        [ObservableProperty]
        private int _onlineTotal;

        private bool _disposedValue;

        public IAsyncRelayCommand PlayCommand { get; }
        public IAsyncRelayCommand DownloadCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IRelayCommand SelectFolderCommand { get; }
        public ObservableCollectionExtended<TrackViewModel> Tracks => _releases;

        public ProgressContainer<Percentage> Progress { get; }
        public Func<(bool IsSuccess, string Folder)>? SelectFolderProxy { get; set; }

        public ShellViewModel(SynchronizationContext synchronizationContext, IMonstercatApi api, IPlaybackService playbackService)
        {
            _downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Monstercat Downloads");
            Directory.CreateDirectory(_downloadPath);

            var filter = this.WhenPropertyChanged(x => x.TextFilter)
                .DistinctUntilChanged()
                .Throttle(TimeSpan.FromMilliseconds(250))
                .Select(x => BuildFilter(x.Value));

            _api = api;
            _playbackService = playbackService;
            _cancellationTokenSource = new CancellationTokenSource();
            _cache = new SourceCache<KeyValuePair<string, Track>, string>(vm => vm.Key);
            _releases = new ObservableCollectionExtended<TrackViewModel>();
            _subscription = _cache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .Filter(filter)
                .Sort(SortExpressionComparer<KeyValuePair<string, Track>>.Ascending(p => p.Value.Title))
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
                    Tags = CreateTags(p.Value),
                    ImageUrl = p.Value.Release.GetSmallCoverArtUri(),
                })
                .ObserveOn(synchronizationContext)
                .Bind(_releases)
                .Subscribe();

            RefreshCommand = new AsyncRelayCommand(Refresh);
            PlayCommand = new AsyncRelayCommand<object?>(Play, CanPlay);
            DownloadCommand = new AsyncRelayCommand(Download, CanDownload);
            SelectFolderCommand = new RelayCommand(SelectFolder);

            Progress = new ProgressContainer<Percentage>();
            _progressService = new DispatcherProgress<Percentage>(synchronizationContext, (p) => Progress.Report(p), TimeSpan.FromMilliseconds(250));

            static ObservableCollection<string> CreateTags(Track track)
            {
                var collection = new ObservableCollection<string>(track.Tags ?? Enumerable.Empty<string>());
                if (!string.IsNullOrWhiteSpace(track.GenrePrimary))
                {
                    collection.Add(track.GenrePrimary);
                }

                if (!string.IsNullOrWhiteSpace(track.GenreSecondary))
                {
                    collection.Add(track.GenreSecondary);
                }

                if (!string.IsNullOrWhiteSpace(track.Brand))
                {
                    collection.Add(track.Brand);
                }

                return collection;
            }

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

        private async Task Download(CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_downloadPath))
            {
                return;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            IsLoading = true;
            IsDialogOpen = false;
            IsDownLoading = true;

            DownloadCommand.NotifyCanExecuteChanged();

            try
            {
                var downloadPath = _downloadPath;
                var localCopy = _cache.Items.Where(p => p.Value.Downloadable).ToArray();
                var current = 0;

                var tasks = new List<Task>();
                foreach (var batch in localCopy.Batch(localCopy.Length / 4))
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var builder = new StringBuilder();
                        foreach (var item in batch)
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }

                            builder.Append(item.Value.ArtistsTitle);
                            builder.Append(" - ");
                            builder.Append(item.Value.Title);

                            if (!string.IsNullOrWhiteSpace(item.Value.Version))
                            {
                                builder.Append('(');
                                builder.Append(item.Value.Version);
                                builder.Append(')');
                            }

                            builder.Append(".flac");

                            var fileName = builder.ToString().SanitizeAsFileName();
                            builder.Clear();

                            var filePath = Path.Combine(downloadPath, fileName!);
                            if (File.Exists(filePath))
                            {
                                current++;
                                _progressService.Report(current, localCopy.Length);
                                continue;
                            }

                            using var stream = await _api.DownloadTrackAsStream(new TrackDownloadRequest()
                            {
                                Format = FileFormat.flac,
                                ReleaseId = item.Value.Release.Id,
                                TrackId = item.Value.Id,
                            });

                            if (token.IsCancellationRequested)
                            {
                                return;
                            }

                            using var writeStream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);

                            await stream.CopyToAsync(writeStream);

                            current++;
                            _progressService.Report(current, localCopy.Length);
                        }
                    }, token));
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                IsDownLoading = false;
                IsLoading = false;
            }
        }

        private bool CanDownload()
        {
            return !_isDownLoading
                && !_isLoading
                && !string.IsNullOrWhiteSpace(_downloadPath)
                && Directory.Exists(_downloadPath);
        }

        private void SelectFolder()
        {
            var proxy = SelectFolderProxy;
            if (proxy is null)
            {
                return;
            }

            var (IsSuccess, Folder) = proxy.Invoke();
            if (!IsSuccess)
            {
                return;
            }

            DownloadPath = Folder;
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
            const int ApiRequestLimit = 100;

            IsLoading = true;
            try
            {
                var result = await _api.SearchTracks(new TrackSearchRequest()
                {
                    Limit = ApiRequestLimit,
                    Skip = 0,
                });

                if (result?.Results is null)
                {
                    _progressService.Report(0, 0);
                    return;
                }

                _onlineTotal = result.Total;

                var count = result.Results.Length;
                _progressService.Report(count, result.Total);
                _cache.AddOrUpdate(result.Results.Select(p => new KeyValuePair<string, Track>($"{p.Release.Id}_{ p.Id}", p)));

                while (count < result.Total || result.Results is null || result.Results.Length == 0)
                {
                    var skip = result.Offset + result.Limit;
                    //System.Diagnostics.Debug.WriteLine($"Offset: {skip} ({count}/{result.Total}) - Limit: {result.Limit}");

                    result = await _api.SearchTracks(new TrackSearchRequest()
                    {
                        Limit = result.Limit,
                        Skip = skip,
                    });

                    if (result?.Results is null)
                    {
                        _progressService.Report(count, count);
                        break;
                    }

                    count += result.Results.Length;
                    _progressService.Report(count, result.Total);
                    _cache.AddOrUpdate(result.Results.Select(p => new KeyValuePair<string, Track>($"{p.Release.Id}_{ p.Id}", p)));
                }
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

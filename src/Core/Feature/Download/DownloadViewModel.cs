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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    [ObservableObject]
    public sealed partial class DownloadViewModel : IDisposable
    {
        private readonly CompositeDisposable _subscription;
        private readonly IMonstercatApi _api;
        private readonly DispatcherProgress<Percentage> _progressService;
        private readonly IObservableCache<TagViewModel, string> _tagCache;

        private readonly ObservableCollectionExtended<TrackViewModel> _tracks;
        private readonly ObservableCollectionExtended<TagViewModel> _tags;
        private readonly ObservableCollectionExtended<TagViewModel> _selectedTags;

        private bool _disposedValue;

        [ObservableProperty]
        private string? _textFilter;

        [AlsoNotifyCanExecuteFor(nameof(DownloadCommand))]
        [ObservableProperty]
        private bool _isDownLoading;

        [AlsoNotifyCanExecuteFor(nameof(DownloadCommand))]
        [ObservableProperty]
        private string? _downloadPath;

        public Func<(bool IsSuccess, string Folder)>? SelectFolderProxy { get; set; }

        public Action? OnDownloadStarted { get; set; }

        public ProgressContainer<Percentage> Progress { get; }

        public ReadOnlyObservableCollection<TrackViewModel> Tracks { get; }

        public ReadOnlyObservableCollection<TagViewModel> Tags { get; }

        public ReadOnlyObservableCollection<TagViewModel> SelectedTags { get; }

        public DownloadViewModel(SynchronizationContext synchronizationContext, IMonstercatApi api, TrackRepository trackRepository)
        {
            _api = api;

            _tracks = new ObservableCollectionExtended<TrackViewModel>();
            _tags = new ObservableCollectionExtended<TagViewModel>();
            _selectedTags = new ObservableCollectionExtended<TagViewModel>();

            Tracks = new ReadOnlyObservableCollection<TrackViewModel>(_tracks);
            Tags = new ReadOnlyObservableCollection<TagViewModel>(_tags);
            SelectedTags = new ReadOnlyObservableCollection<TagViewModel>(_selectedTags);

            Progress = new ProgressContainer<Percentage>();
            _progressService = new DispatcherProgress<Percentage>(synchronizationContext, (p) => Progress.Report(p), TimeSpan.FromMilliseconds(250));

            var textFilter = this.WhenPropertyChanged(x => x.TextFilter)
                .DistinctUntilChanged()
                .Throttle(TimeSpan.FromMilliseconds(250))
                .Select(x => BuildTextFilter(x.Value));

            var tags = trackRepository
                .ConnectTracks()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .TransformMany(p => p.Value.Tags, p => p)
                .SortBy(p => p)
                .Transform(p => new TagViewModel() { Value = p, IsSelected = false });

            _tagCache = tags.AsObservableCache();

            var selectedTagSubscription = _tagCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .AutoRefresh()
                .Filter(x => x.IsSelected)
                .ObserveOn(synchronizationContext)
                .Bind(_selectedTags)
                .Subscribe();

            var tagSubscription = _tagCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .Sort(SortExpressionComparer<TagViewModel>
                    .Ascending(p => p.Value))
                .ObserveOn(synchronizationContext)
                .Bind(_tags, new AddingObservableCollectionAdaptor<TagViewModel, string>())
                .Subscribe();

            var trackSubscription = trackRepository
                .ConnectTracks()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .AutoRefreshOnObservable(_ => _selectedTags.ObserveCollectionChanges(), TimeSpan.FromMilliseconds(250))
                .Filter(textFilter)
                .Filter(BuildTagFilter(_selectedTags))
                .Sort(SortExpressionComparer<KeyValuePair<string, Track>>
                    .Ascending(p => p.Value.Title)
                    .ThenByAscending(p => p.Value.ArtistsTitle)
                    .ThenByAscending(p => p.Value.Release.CatalogId))
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

            _subscription = new CompositeDisposable(tagSubscription, trackSubscription, selectedTagSubscription);

            static Func<KeyValuePair<string, Track>, bool> BuildTagFilter(IEnumerable<TagViewModel>? tags)
            {
                return vm =>
                {
                    if (tags?.Any() != true)
                    {
                        return true;
                    }

                    if (vm.Value.Tags is null || vm.Value.Tags.Length == 0)
                    {
                        return false;
                    }

                    return tags.Select(p => p.Value).Intersect(vm.Value.Tags).Any();
                };
            }

            static Func<KeyValuePair<string, Track>, bool> BuildTextFilter(string? searchText)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    return _ => true;
                }

                return vm => vm.Value.Title.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) > -1
                    || vm.Value.ArtistsTitle.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) > -1;
            }
        }

        [ICommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanDownload), IncludeCancelCommand = true)]
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

            OnDownloadStarted?.Invoke();

            IsDownLoading = true;

            DownloadCommand.NotifyCanExecuteChanged();

            try
            {
                var downloadPath = _downloadPath;
                var localCopy = Tracks.Where(p => p.Downloadable).ToArray();
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

                            builder.Append(item.ArtistsTitle);
                            builder.Append(" - ");
                            builder.Append(item.Title);

                            if (!string.IsNullOrWhiteSpace(item.Version))
                            {
                                builder.Append('(');
                                builder.Append(item.Version);
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
                                ReleaseId = item.ReleaseId,
                                TrackId = item.Id,
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
            }
        }

        private bool CanDownload()
        {
            return !_isDownLoading
                && !string.IsNullOrWhiteSpace(_downloadPath)
                && Directory.Exists(_downloadPath);
        }

        [ICommand]
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

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _subscription.Dispose();
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

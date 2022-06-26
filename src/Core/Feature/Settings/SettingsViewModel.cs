using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using DynamicData.Binding;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class SettingsViewModel : ObservableValidator, IDisposable
    {
        private readonly CompositeDisposable _subscription;
        private readonly SettingsService _settingsService;
        private readonly IObservableCache<TagViewModel, string> _tagCache;
        private readonly ObservableCollectionExtended<TagViewModel> _tags;
        private readonly ObservableCollectionExtended<TagViewModel> _selectedTags;

        private bool _disposedValue;

        private string? _password;

        private string? _email;

        [ObservableProperty]
        private int _parallelDownloads;

        [ObservableProperty]
        private string? _downloadTracksPath;

        [ObservableProperty]
        private string? _downloadImagesPath;

        [ObservableProperty]
        private int _artistsCount;

        [ObservableProperty]
        private int _genresCount;

        [ObservableProperty]
        private int _releasesCount;

        [ObservableProperty]
        private int _tagsCount;

        [ObservableProperty]
        private FileFormat _downloadFileFormat;

        public int MaxParallelDownloads { get; } = Environment.ProcessorCount;
        public ReadOnlyObservableCollection<TagViewModel> Tags { get; }

        public ReadOnlyObservableCollection<TagViewModel> SelectedTags { get; }

        public Func<(bool IsSuccess, string Folder)>? SelectFolderProxy { get; set; }

        public Action? OnSuccssfulSave { get; set; }

        public SettingsViewModel(
            SynchronizationContext synchronizationContext,
            SettingsService settingsService,
            TrackRepository trackRepository,
            IMessenger messenger)
        {
            if (synchronizationContext is null)
            {
                throw new ArgumentNullException(nameof(synchronizationContext));
            }

            if (trackRepository is null)
            {
                throw new ArgumentNullException(nameof(trackRepository));
            }

            if (messenger is null)
            {
                throw new ArgumentNullException(nameof(messenger));
            }

            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _tags = new ObservableCollectionExtended<TagViewModel>();
            _selectedTags = new ObservableCollectionExtended<TagViewModel>();

            Tags = new ReadOnlyObservableCollection<TagViewModel>(_tags);
            SelectedTags = new ReadOnlyObservableCollection<TagViewModel>(_selectedTags);

            _tagCache = trackRepository
                .ConnectUnfilteredTagViewModels()
                .AsObservableCache();

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

            _subscription = new CompositeDisposable(tagSubscription, selectedTagSubscription);

            // messages
            messenger.Register<SettingsViewModel, LoginChangedMessage>(this, (r, m) =>
            {
                r._password = m.Settings.Password;
                r._email = m.Settings.Email;

                SaveCommand.NotifyCanExecuteChanged();
            });
        }

        [RelayCommand]
        public async Task Load()
        {
            await _settingsService.Load();

            _password = _settingsService.Password;
            _email = _settingsService.Email;

            DownloadTracksPath = _settingsService.DownloadTracksPath;
            DownloadImagesPath = _settingsService.DownloadImagesPath;
            DownloadFileFormat = _settingsService.DownloadFileFormat;
            ParallelDownloads = _settingsService.ParallelDownloads;

            ArtistsCount = _settingsService.ArtistsCount;
            GenresCount = _settingsService.GenresCount;
            ReleasesCount = _settingsService.ReleasesCount;
            TagsCount = _settingsService.TagsCount;
        }

        [RelayCommand]
        private async Task Save()
        {
            _settingsService.Password = _password;
            _settingsService.Email = _email;

            _settingsService.DownloadTracksPath = DownloadTracksPath;
            _settingsService.DownloadImagesPath = DownloadImagesPath;
            _settingsService.DownloadFileFormat = DownloadFileFormat;
            _settingsService.ParallelDownloads = ParallelDownloads;

            _settingsService.ExcludedTags = SelectedTags
                .Where(p => p.IsSelected)
                .Select(p => p.Value)
                .ToArray();

            _settingsService.ArtistsCount = ArtistsCount;
            _settingsService.GenresCount = GenresCount;
            _settingsService.ReleasesCount = ReleasesCount;
            _settingsService.TagsCount = TagsCount;

            await _settingsService.Save();

            OnSuccssfulSave?.Invoke();
        }

        [RelayCommand]
        private void SelectTrackDownloadFolder()
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

            DownloadTracksPath = Folder;
        }

        [RelayCommand]
        private void SelectImageDownloadFolder()
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

            DownloadImagesPath = Folder;
        }

        [RelayCommand]
        private async Task ResetSettings()
        {
            await _settingsService.ResetSettings();
            await Load();
        }

        [RelayCommand]
        private async Task ResetCredentials()
        {
            await _settingsService.ResetCredentials();
            await Load();
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

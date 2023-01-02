using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Gress;
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class ShellViewModel : ObservableRecipient, IDisposable
    {
        private readonly ITrackRepository _trackRepository;
        private readonly IDisposable _subscription;

        private bool _disposedValue;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
        private bool _isLoading;

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get { return _isLoggedIn; }
            private set { SetProperty(ref _isLoggedIn, value); }
        }

        public string Title { get; }

        public SettingsViewModel Settings { get; }

        public AboutViewModel About { get; }

        public DownloadViewModel Downloads { get; }

        public ReleasesViewModel Releases { get; }

        public GenresViewModel Genres { get; }

        public TagsViewModel Tags { get; }

        public TracksViewModel Tracks { get; }

        public ArtistsViewModel Artists { get; }

        public PlaybackViewModel Playback { get; }

        public ProgressContainer<Percentage> Progress { get; }

        public BrandViewModel<Silk> Silk { get; }

        public BrandViewModel<Uncaged> Uncaged { get; }

        public BrandViewModel<Instinct> Instinct { get; }

        public ShellViewModel(
            SynchronizationContext synchronizationContext,
            AboutViewModel about,
            SettingsViewModel settings,
            ReleasesViewModel releases,
            TagsViewModel tags,
            TracksViewModel tracks,
            GenresViewModel genres,
            ArtistsViewModel artists,
            ITrackRepository trackRepository,
            PlaybackViewModel playback,
            DownloadViewModel downloadViewModel,
            ProgressContainer<Percentage> progress,
            BrandViewModel<Silk> silk,
            BrandViewModel<Uncaged> uncaged,
            BrandViewModel<Instinct> instinct,
            IMessenger messenger)
            : base(messenger)
        {
            About = about ?? throw new ArgumentNullException(nameof(about));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Releases = releases ?? throw new ArgumentNullException(nameof(releases));
            Tags = tags ?? throw new ArgumentNullException(nameof(tags));
            Tracks = tracks ?? throw new ArgumentNullException(nameof(tracks));
            Genres = genres ?? throw new ArgumentNullException(nameof(genres));
            Artists = artists ?? throw new ArgumentNullException(nameof(artists));
            _trackRepository = trackRepository ?? throw new ArgumentNullException(nameof(trackRepository));
            Playback = playback ?? throw new ArgumentNullException(nameof(playback));
            Downloads = downloadViewModel ?? throw new ArgumentNullException(nameof(downloadViewModel));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            Silk = silk ?? throw new ArgumentNullException(nameof(silk));
            Uncaged = uncaged ?? throw new ArgumentNullException(nameof(uncaged));
            Instinct = instinct ?? throw new ArgumentNullException(nameof(instinct));

            Title = $"v{About.AssemblyVersionString} SoftThorn.Monstercat.Browser.Wpf ";

            // messages
            messenger.Register<ShellViewModel, LoginChangedMessage>(this, (r, m) => r.IsLoggedIn = m.IsLoggedIn);

            _subscription = _trackRepository
                .ConnectTracks()
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(synchronizationContext)
                .Do(_ => PlayCommand.NotifyCanExecuteChanged())
                .Subscribe();
        }

        [RelayCommand(CanExecute = nameof(CanPlay))]
        public void Play(object? args)
        {
            switch (args)
            {
                case TrackViewModel track:
                    Playback.Add(track);
                    return;

                case ReleaseViewModel release:
                    Playback.Add(release.Tracks);
                    break;

                case ArtistViewModel artist:
                    Playback.Add(artist.Tracks);
                    break;

                case BrandViewModel<Silk> silk:
                    Playback.Add(silk.Releases);
                    break;

                case BrandViewModel<Uncaged> uncaged:
                    Playback.Add(uncaged.Releases);
                    break;

                case BrandViewModel<Instinct> instinct:
                    Playback.Add(instinct.Releases);
                    break;
            }
        }

        private static bool CanPlay(object? args)
        {
            return args is TrackViewModel
                || args is ReleaseViewModel
                || args is ArtistViewModel
                || args is BrandViewModel<Silk>
                || args is BrandViewModel<Uncaged>
                || args is BrandViewModel<Instinct>;
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task Refresh()
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

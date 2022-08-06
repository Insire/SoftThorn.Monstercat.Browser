using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Gress;
using System;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class ShellViewModel : ObservableRecipient
    {
        private readonly ITrackRepository _trackRepository;

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

        public ArtistsViewModel Artists { get; }

        public PlaybackViewModel Playback { get; }

        public ProgressContainer<Percentage> Progress { get; }

        public BrandViewModel<Silk> Silk { get; }

        public BrandViewModel<Uncaged> Uncaged { get; }

        public BrandViewModel<Instinct> Instinct { get; }

        public ShellViewModel(
            AboutViewModel about,
            SettingsViewModel settings,
            ReleasesViewModel releases,
            TagsViewModel tags,
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
            messenger.Register<ShellViewModel, LoginChangedMessage>(this, (r, m) =>
            {
                r.IsLoggedIn = m.IsLoggedIn;
            });
        }

        [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanPlay))]
        public async Task Play(object? args)
        {
            switch (args)
            {
                case TrackViewModel track:
                    await Playback.Add(track);
                    return;

                case ReleaseViewModel release:
                    await Playback.Add(release.Tracks);
                    break;

                case ArtistViewModel artist:
                    await Playback.Add(artist.Tracks);
                    break;
            }
        }

        private static bool CanPlay(object? args)
        {
            return args is TrackViewModel
                || args is ReleaseViewModel
                || args is ArtistViewModel;
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
    }
}

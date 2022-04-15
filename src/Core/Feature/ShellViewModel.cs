using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gress;
using SoftThorn.MonstercatNet;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    [ObservableObject]
    public sealed partial class ShellViewModel
    {
        private readonly TrackRepository _trackRepository;
        private readonly IPlaybackService _playbackService;

        [ObservableProperty]
        [AlsoNotifyCanExecuteFor(nameof(RefreshCommand))]
        private bool _isLoading;

        public DownloadViewModel Downloads { get; }

        public LoginViewModel Login { get; }

        public ReleasesViewModel Releases { get; }

        public TagsViewModel Tags { get; }

        public ProgressContainer<Percentage> Progress { get; }

        public ShellViewModel(ReleasesViewModel releases,
                              TagsViewModel tags,
                              TrackRepository trackRepository,
                              IPlaybackService playbackService,
                              DownloadViewModel downloadViewModel,
                              LoginViewModel login,
                              ProgressContainer<Percentage> progress)
        {
            Releases = releases ?? throw new ArgumentNullException(nameof(releases));
            Tags = tags ?? throw new ArgumentNullException(nameof(tags));
            _trackRepository = trackRepository ?? throw new ArgumentNullException(nameof(trackRepository));
            _playbackService = playbackService ?? throw new ArgumentNullException(nameof(playbackService));
            Downloads = downloadViewModel ?? throw new ArgumentNullException(nameof(downloadViewModel));
            Login = login ?? throw new ArgumentNullException(nameof(login));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        [ICommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanPlay))]
        public async Task Play(object? args)
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

        [ICommand(AllowConcurrentExecutions = false)]
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

        public async Task<bool> TryLogin(Action? handleLoginValidationErrors, CancellationToken token)
        {
            if (Login.IsLoggedIn)
            {
                return Login.IsLoggedIn;
            }

            Login.Validate();
            if (Login.HasErrors)
            {
                handleLoginValidationErrors?.Invoke();
            }
            else
            {
                await Login.Login(token);
            }

            return Login.IsLoggedIn;
        }
    }
}

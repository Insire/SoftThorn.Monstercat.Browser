using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Gress;
using Serilog;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class DownloadViewModel : ObservableRecipient, IDisposable
    {
        private readonly DownloadService _downloadService;
        private readonly DispatcherProgressFactory<Percentage> _dispatcherProgressFactory;
        private readonly DispatcherProgress<Percentage> _progressService;

        private int _parallelDownloads;
        private string? _downloadTracksPath;
        private FileFormat _downloadFileFormat;
        private bool _disposedValue;

        [ObservableProperty]
        private bool _isDownLoading;

        private int _tracksToDownload;
        public int TracksToDownload
        {
            get { return _tracksToDownload; }
            private set { SetProperty(ref _tracksToDownload, value); }
        }

        public ProgressContainer<Percentage> Progress { get; }

        public DownloadViewModel(
            IMessenger messenger,
            DownloadService downloadService,
            DispatcherProgressFactory<Percentage> dispatcherProgressFactory)
            : base(messenger)
        {
            _downloadService = downloadService;
            _dispatcherProgressFactory = dispatcherProgressFactory;

            Progress = new ProgressContainer<Percentage>();
            _progressService = _dispatcherProgressFactory.Create((p) => Progress.Report(p));

            // messages
            Messenger.Register<DownloadViewModel, SettingsChangedMessage>(this, (r, m) =>
            {
                r._downloadTracksPath = m.Value.DownloadTracksPath;
                r._downloadFileFormat = m.Value.DownloadFileFormat;
                r._parallelDownloads = m.Value.ParallelDownloads;
            });

            Messenger.Register<DownloadViewModel, DownloadTracksMessage>(this, async (r, m)
                => await Task.Run(async () => await r.Download(m.Value, CancellationToken.None)));
        }

        private async Task Download(IReadOnlyCollection<TrackViewModel> tracks, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_downloadTracksPath))
            {
                return;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            try
            {
                IsDownLoading = true;
                TracksToDownload = tracks.Count;

                await _downloadService.Download(new DownloadOptions()
                {
                    DownloadFileFormat = _downloadFileFormat,
                    DownloadTracksPath = _downloadTracksPath,
                    ParallelDownloads = _parallelDownloads,
                    Tracks = tracks,
                }, _progressService, token);
            }
            finally
            {
                TracksToDownload = 0;
                IsDownLoading = false;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _progressService.Dispose();
                    Messenger.UnregisterAll(this);
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

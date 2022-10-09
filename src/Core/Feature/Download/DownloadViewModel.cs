using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Gress;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class DownloadViewModel : ObservableRecipient, IDisposable
    {
        private readonly IMonstercatApi _api;
        private readonly ILogger _log;
        private readonly DispatcherProgressFactory<Percentage> _dispatcherProgressFactory;
        private readonly DispatcherProgress<Percentage> _progressService;
        private readonly ObjectPool<StringBuilder> _objectPool;

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
            IMonstercatApi api,
            IMessenger messenger,
            ILogger log,
            DispatcherProgressFactory<Percentage> dispatcherProgressFactory,
            ObjectPool<StringBuilder> objectPool)
            : base(messenger)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _log = log?.ForContext<DownloadViewModel>() ?? throw new ArgumentNullException(nameof(log));
            _dispatcherProgressFactory = dispatcherProgressFactory ?? throw new ArgumentNullException(nameof(dispatcherProgressFactory));
            _objectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));

            Progress = new ProgressContainer<Percentage>();
            _progressService = _dispatcherProgressFactory.Create((p) => Progress.Report(p));

            // messages
            Messenger.Register<DownloadViewModel, SettingsChangedMessage>(this, (r, m) =>
            {
                r._downloadTracksPath = m.Value.DownloadTracksPath;
                r._downloadFileFormat = m.Value.DownloadFileFormat;
                r._parallelDownloads = m.Value.ParallelDownloads;
            });

            Messenger.Register<DownloadViewModel, DownloadTracksMessage>(this, async (r, m) =>
            {
                await r.Download(m.Value, CancellationToken.None);
            });
        }

        public async Task Download(IReadOnlyCollection<TrackViewModel> tracks, CancellationToken token)
        {
            var parallelDownloads = _parallelDownloads;
            var fileFormat = _downloadFileFormat;
            var downloadPath = _downloadTracksPath;

            if (string.IsNullOrWhiteSpace(downloadPath))
            {
                return;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            IsDownLoading = true;
            TracksToDownload = tracks.Count;
            var files = Directory.GetFiles(downloadPath, "*" + fileFormat.GetFileExtension());

            try
            {
                var current = 0;

                var tasks = new List<Task>();
                foreach (var batch in tracks.Batch(TracksToDownload / parallelDownloads))
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        foreach (var item in batch)
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }

                            var builder = _objectPool.Get();
                            var filePath = item.GetFilePath(builder, downloadPath, fileFormat);
                            _objectPool.Return(builder);

                            if (files.Contains(filePath))
                            {
                                current++;
                                _progressService.Report(current, TracksToDownload);
                                continue;
                            }

                            _log.Information("Downloading {TrackId} {ReleaseId} to {FilePath}", item.Release.Id, item.Id, filePath);
                            using var stream = await _api.DownloadTrackAsStream(new TrackDownloadRequest()
                            {
                                Format = _downloadFileFormat,
                                ReleaseId = item.Release.Id,
                                TrackId = item.Id,
                            });

                            if (token.IsCancellationRequested)
                            {
                                return;
                            }

                            using var writeStream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);

                            await stream.CopyToAsync(writeStream);

                            current++;
                            _progressService.Report(current, TracksToDownload);
                        }
                    }, token));
                }

                await Task.WhenAll(tasks);
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

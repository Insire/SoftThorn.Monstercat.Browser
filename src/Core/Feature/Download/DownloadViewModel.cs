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
        private readonly IToastService _toastService;
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
            IToastService toastService,
            DispatcherProgressFactory<Percentage> dispatcherProgressFactory,
            ObjectPool<StringBuilder> objectPool)
            : base(messenger)
        {
            _api = api;
            _toastService = toastService;
            _log = log.ForContext<DownloadViewModel>();
            _dispatcherProgressFactory = dispatcherProgressFactory;
            _objectPool = objectPool;

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
                await Task.Run(async () => await r.Download(m.Value, CancellationToken.None));
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

            var fileLookup = Directory
                .GetFiles(downloadPath, "*" + fileFormat.GetFileExtension())
                .ToDictionary(p => p.ToLowerInvariant());

            var notDownloadedTracks = tracks
                .Select(item =>
                {
                    var builder = _objectPool.Get();
                    var filePath = item.GetFilePath(builder, downloadPath, fileFormat);

                    builder.Clear();
                    _objectPool.Return(builder);

                    return (filePath, item);
                })
                .Where(tuple => !fileLookup.ContainsKey(tuple.filePath.ToLowerInvariant()))
                .ToList();

            try
            {
                var current = 0;
                var batchSize = 1;
                if (notDownloadedTracks.Count > parallelDownloads)
                {
                    batchSize = Convert.ToInt32(Math.Round(Convert.ToDouble(notDownloadedTracks.Count) / Convert.ToDouble(parallelDownloads), MidpointRounding.AwayFromZero));
                }

                var batches = notDownloadedTracks.Batch(batchSize).ToList();
                var tasks = new List<Task>();
                for (var i = 0; i < batches.Count; i++)
                {
                    var batch = batches[i];
                    tasks.Add(Task.Run(async () =>
                    {
                        foreach (var tuple in batch)
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }

                            var item = tuple.item;
                            var filePath = tuple.filePath;

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
                            _progressService.Report(current, notDownloadedTracks.Count);
                        }
                    }, token));
                }

                await Task.WhenAll(tasks);

                switch (notDownloadedTracks.Count)
                {
                    case 0:
                        _toastService.Show(new ToastViewModel("Downloading finished", "You were already up to date", ToastType.Information, true));
                        break;

                    case 1:
                        _toastService.Show(new ToastViewModel("Download complete", "1 track downloaded", ToastType.Information, true));
                        break;

                    default:
                        _toastService.Show(new ToastViewModel("Download complete", $"{notDownloadedTracks.Count} tracks downloaded", ToastType.Information, true));
                        break;
                }
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

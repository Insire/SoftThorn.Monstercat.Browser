using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Gress;
using Serilog;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Generic;
using System.IO;
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

        public DownloadViewModel(IMonstercatApi api, IMessenger messenger, ILogger log, DispatcherProgressFactory<Percentage> dispatcherProgressFactory)
            : base(messenger)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _log = log.ForContext<DownloadViewModel>();
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

            try
            {
                var current = 0;

                var tasks = new List<Task>();
                foreach (var batch in tracks.Batch(TracksToDownload / parallelDownloads))
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

                            var filePath = GetFilePath(builder, item, downloadPath, fileFormat);
                            if (File.Exists(filePath))
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

        private static string GetFilePath(StringBuilder builder, TrackViewModel track, string downloadPath, FileFormat fileFormat)
        {
            builder.Clear();
            builder.Append(track.ArtistsTitle);
            builder.Append(" - ");
            builder.Append(track.Title);

            if (!string.IsNullOrWhiteSpace(track.Version))
            {
                builder.Append('(');
                builder.Append(track.Version);
                builder.Append(')');
            }

            builder.Append(GetFileExtension(fileFormat));

            var fileName = builder.ToString().SanitizeAsFileName();

            return Path.Combine(downloadPath, fileName!);
        }

        private static string GetFileExtension(FileFormat fileFormat)
        {
            return fileFormat switch
            {
                FileFormat.flac => ".flac",
                FileFormat.mp3 => ".mp3",
                FileFormat.wav => ".wav",
                _ => throw new NotImplementedException(),
            };
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

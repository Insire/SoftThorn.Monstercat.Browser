using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Gress;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class DownloadViewModel : ObservableRecipient
    {
        private readonly IMonstercatApi _api;
        private readonly DispatcherProgress<Percentage> _progressService;

        private int _parallelDownloads;
        private string? _downloadTracksPath;
        private FileFormat _downloadFileFormat;

        [ObservableProperty]
        private bool _isDownLoading;

        private int _tracksToDownload;
        public int TracksToDownload
        {
            get { return _tracksToDownload; }
            private set { SetProperty(ref _tracksToDownload, value); }
        }

        public ProgressContainer<Percentage> Progress { get; }

        public DownloadViewModel(SynchronizationContext synchronizationContext, IMonstercatApi api, IMessenger messenger)
            : base(messenger)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));

            Progress = new ProgressContainer<Percentage>();
            _progressService = new DispatcherProgress<Percentage>(synchronizationContext, (p) => Progress.Report(p), TimeSpan.FromMilliseconds(250));

            // messages
            messenger.Register<DownloadViewModel, SettingsChangedMessage>(this, (r, m) =>
            {
                r._downloadTracksPath = m.Settings.DownloadTracksPath;
                r._downloadFileFormat = m.Settings.DownloadFileFormat;
                r._parallelDownloads = m.Settings.ParallelDownloads;
            });

            messenger.Register<DownloadViewModel, DownloadTracksMessage>(this, async (r, m) =>
            {
                await r.Download(m.Tracks, CancellationToken.None);
            });
        }

        private async Task Download(IReadOnlyCollection<TrackViewModel> tracks, CancellationToken token)
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
                foreach (var batch in tracks.Batch(tracks.Count / parallelDownloads))
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
                                _progressService.Report(current, tracks.Count);
                                continue;
                            }

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
                            _progressService.Report(current, tracks.Count);
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
    }
}

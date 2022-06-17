using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Gress;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class DownloadViewModel : ObservableRecipient
    {
        private readonly IMonstercatApi _api;
        private readonly DispatcherProgress<Percentage> _progressService;

        private string? _downloadTracksPath;

        [ObservableProperty]
        private bool _isDownLoading;

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
            });

            messenger.Register<DownloadViewModel, DownloadTracksMessage>(this, async (r, m) =>
            {
                await r.Download(m.Tracks, CancellationToken.None);
            });
        }

        private async Task Download(IReadOnlyCollection<TrackViewModel> tracks, CancellationToken token)
        {
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

            try
            {
                var current = 0;

                var tasks = new List<Task>();
                foreach (var batch in tracks.Batch(tracks.Count / 4))
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
                                _progressService.Report(current, tracks.Count);
                                continue;
                            }

                            using var stream = await _api.DownloadTrackAsStream(new TrackDownloadRequest()
                            {
                                Format = FileFormat.flac,
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
                IsDownLoading = false;
            }
        }
    }
}

using Gress;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class DownloadService
    {
        private readonly ObjectPool<StringBuilder> _objectPool;
        private readonly ILogger _log;
        private readonly IFileSystemService _fileSystemService;
        private readonly IMonstercatApi _api;
        private readonly IToastService _toastService;

        public DownloadService(
            IFileSystemService fileSystemService,
            ILogger log,
            IMonstercatApi api,
            IToastService toastService,
            ObjectPool<StringBuilder> objectPool)
        {
            _fileSystemService = fileSystemService;
            _log = log.ForContext<DownloadService>();
            _api = api;
            _toastService = toastService;
            _objectPool = objectPool;
        }

        public async Task Download(DownloadOptions options, IProgress<Percentage> progressService, CancellationToken token)
        {
            progressService.Report(0, 1);

            var fileLookup = _fileSystemService
                .DirectoryGetFiles(options.DownloadTracksPath, "*" + options.DownloadFileFormat.GetFileExtension())
                .ToDictionary(p => p.ToLowerInvariant());

            var notDownloadedTracks = options.Tracks
                .Select(item =>
                {
                    var builder = _objectPool.Get();
                    var filePath = item.GetFilePath(builder, options.DownloadTracksPath, options.DownloadFileFormat);

                    builder.Clear();
                    _objectPool.Return(builder);

                    return (filePath, item);
                })
                .Where(tuple => !fileLookup.ContainsKey(tuple.filePath.ToLowerInvariant()))
                .ToList();

            var currentProgress = 0;
            var totalPogress = notDownloadedTracks.Count * 2;
            var batchSize = 1;
            if (notDownloadedTracks.Count > options.ParallelDownloads)
            {
                batchSize = Convert.ToInt32(Math.Round(Convert.ToDouble(notDownloadedTracks.Count) / Convert.ToDouble(options.ParallelDownloads), MidpointRounding.AwayFromZero));
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
                        currentProgress++;
                        progressService.Report(currentProgress, totalPogress);

                        using var stream = await _api.DownloadTrackAsStream(new TrackDownloadRequest()
                        {
                            Format = options.DownloadFileFormat,
                            ReleaseId = item.Release.Id,
                            TrackId = item.Id,
                        });

                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        using var writeStream = _fileSystemService.FileOpen(filePath);

                        await stream.CopyToAsync(writeStream);

                        currentProgress++;
                        progressService.Report(currentProgress, totalPogress);
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
    }
}

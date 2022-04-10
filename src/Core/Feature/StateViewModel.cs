using DynamicData;
using Gress;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class TrackRepository : IDisposable
    {
        private readonly IMonstercatApi _api;
        private readonly SourceCache<KeyValuePair<string, Track>, string> _cache;
        private readonly DispatcherProgress<Percentage> _progressService;
        private bool _disposedValue;

        public TrackRepository(DispatcherProgress<Percentage> progressService, IMonstercatApi api)
        {
            _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
            _api = api ?? throw new ArgumentNullException(nameof(api));

            _cache = new SourceCache<KeyValuePair<string, Track>, string>(vm => vm.Key);
        }

        public IObservable<IChangeSet<KeyValuePair<string, Track>, string>> Connect()
        {
            return _cache.Connect();
        }

        public async Task Refresh()
        {
            const int ApiRequestLimit = 100;

            var total = 0;
            var skip = 0;
            var localLimit = ApiRequestLimit;
            var result = await _api.SearchTracks(new TrackSearchRequest()
            {
                Limit = localLimit,
                Skip = skip,
            });

            if (result?.Results is null)
            {
                _progressService.Report(0, 0);
                return;
            }

            total = result.Total;
            localLimit = result.Limit;
            skip = result.Offset + localLimit;

            var count = result.Results.Length;
            _progressService.Report(count, total);
            _cache.AddOrUpdate(result.Results.Select(p => new KeyValuePair<string, Track>($"{p.Release.Id}_{ p.Id}", p)));

            var requests = new List<TrackSearchRequest>();

            while (skip < total)
            {
                requests.Add(new TrackSearchRequest()
                {
                    Limit = localLimit,
                    Skip = skip,
                });
                skip += localLimit;
            }

            var tasks = new List<Task>();
            foreach (var batch in requests.Batch(8))
            {
                tasks.Add(Task.Run(async () =>
                {
                    foreach (var request in batch)
                    {
                        var result = await _api.SearchTracks(request);
                        count += result.Results.Length;
                        _progressService.Report(count, total);
                        _cache.AddOrUpdate(result.Results.Select(p => new KeyValuePair<string, Track>($"{p.Release.Id}_{ p.Id}", p)));
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _cache.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
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

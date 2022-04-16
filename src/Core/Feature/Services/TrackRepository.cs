using DynamicData;
using Gress;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class TrackRepository : IDisposable
    {
        private readonly IMonstercatApi _api;
        private readonly SourceCache<KeyValuePair<string, List<Track>>, string> _tagCache;
        private readonly SourceCache<KeyValuePair<string, Track>, string> _trackCache;
        private readonly DispatcherProgress<Percentage> _progressService;

        private bool _disposedValue;

        public TrackRepository(DispatcherProgress<Percentage> progressService, IMonstercatApi api)
        {
            _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
            _api = api ?? throw new ArgumentNullException(nameof(api));

            _trackCache = new SourceCache<KeyValuePair<string, Track>, string>(vm => vm.Key);
            _tagCache = new SourceCache<KeyValuePair<string, List<Track>>, string>(vm => vm.Key);
        }

        public IObservable<IChangeSet<KeyValuePair<string, Track>, string>> ConnectTracks()
        {
            return _trackCache.Connect();
        }

        public IObservable<IChangeSet<KeyValuePair<string, List<Track>>, string>> ConnectTags()
        {
            return _tagCache.Connect();
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
            _trackCache.AddOrUpdate(result.Results.Select(p => new KeyValuePair<string, Track>($"{p.Release.Id}_{ p.Id}", p)));

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

            var tags = new ConcurrentDictionary<string, ConcurrentStack<Track>>();
            var tracks = new ConcurrentBag<KeyValuePair<string, Track>>();
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

                        foreach (var track in result.Results)
                        {
                            track.Tags = track.Tags?.Select(p => p.Trim().ToLowerInvariant()).ToArray();

                            tracks.Add(new KeyValuePair<string, Track>($"{track.Release.Id}_{ track.Id}", track));
                            if (track.Tags is null || track.Tags.Length == 0)
                            {
                                continue;
                            }

                            foreach (var tag in track.Tags)
                            {
                                tags.AddOrUpdate(tag, new ConcurrentStack<Track>(new[] { track }), (key, list) =>
                                   {
                                       list.Push(track);
                                       return list;
                                   });
                            }
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            _trackCache.AddOrUpdate(tracks);
            _tagCache.AddOrUpdate(tags.Select(p => new KeyValuePair<string, List<Track>>(p.Key, p.Value.ToList())));
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _trackCache.Dispose();
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

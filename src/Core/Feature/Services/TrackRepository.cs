using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using DynamicData.Binding;
using Gress;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class TrackRepository : IDisposable, ITrackRepository
    {
        private readonly IMonstercatApi _api;
        private readonly SourceCache<KeyValuePair<string, List<TrackViewModel>>, string> _tagCache;
        private readonly SourceCache<KeyValuePair<string, List<TrackViewModel>>, string> _genreCache;
        private readonly SourceCache<ArtistViewModel, Guid> _artistCache;
        private readonly SourceCache<TrackViewModel, string> _trackCache;
        private readonly SourceCache<ReleaseViewModel, Guid> _releaseCache;
        private readonly SourceCache<TagViewModel, string> _excludedTags;
        private readonly DispatcherProgress<Percentage> _progressService;

        private string[] _excludedTagValues;
        private bool _disposedValue;

        public TrackRepository(DispatcherProgress<Percentage> progressService, IMonstercatApi api, IMessenger messenger)
        {
            _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
            _api = api ?? throw new ArgumentNullException(nameof(api));

            _excludedTagValues = Array.Empty<string>();
            _excludedTags = new SourceCache<TagViewModel, string>(p => p.Value);
            _trackCache = new SourceCache<TrackViewModel, string>(p => p.Key);
            _tagCache = new SourceCache<KeyValuePair<string, List<TrackViewModel>>, string>(p => p.Key);
            _genreCache = new SourceCache<KeyValuePair<string, List<TrackViewModel>>, string>(p => p.Key);
            _artistCache = new SourceCache<ArtistViewModel, Guid>(p => p.Id);
            _releaseCache = new SourceCache<ReleaseViewModel, Guid>(p => p.Id);

            // messages
            messenger.Register<TrackRepository, SettingsChangedMessage>(this, (r, m) =>
            {
                r._excludedTags.Edit(list =>
                {
                    r._excludedTagValues = m.Value.ExcludedTags;
                    list.Load(m.Value.ExcludedTags.Select(p => new TagViewModel() { Value = p }));
                });
            });
        }

        public IObservable<IChangeSet<ReleaseViewModel, Guid>> ConnectReleases()
        {
            return _releaseCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged();
        }

        public IObservable<IChangeSet<TrackViewModel, string>> ConnectTracks()
        {
            return _trackCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged();
        }

        public IObservable<IChangeSet<KeyValuePair<string, List<TrackViewModel>>, string>> ConnectGenres()
        {
            return _genreCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .SortBy(p => p.Key, SortDirection.Descending, SortOptimisations.ComparesImmutableValuesOnly);
        }

        public IObservable<IChangeSet<ArtistViewModel, Guid>> ConnectArtists()
        {
            return _artistCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .SortBy(p => p.LatestReleaseDate, SortDirection.Ascending, SortOptimisations.ComparesImmutableValuesOnly);
        }

        public IObservable<IChangeSet<KeyValuePair<string, List<TrackViewModel>>, string>> ConnectTags()
        {
            return _tagCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .AutoRefreshOnObservable(_ => _excludedTags.Connect())
                .Filter(tag => !_excludedTags.Lookup(tag.Key).HasValue)
                .SortBy(p => p.Key, SortDirection.Descending, SortOptimisations.ComparesImmutableValuesOnly);
        }

        public IObservable<IChangeSet<TagViewModel, string>> ConnectFilteredTagViewModels()
        {
            return _tagCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .AutoRefreshOnObservable(_ => _excludedTags.Connect())
                .Filter(tag => !_excludedTags.Lookup(tag.Key).HasValue)
                .SortBy(p => p.Key, SortDirection.Descending, SortOptimisations.ComparesImmutableValuesOnly)
                .Transform(p => new TagViewModel() { Value = p.Key, IsSelected = false })
            ;
        }

        public IObservable<IChangeSet<TagViewModel, string>> ConnectUnfilteredTagViewModels()
        {
            return _tagCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .SortBy(p => p.Key, SortDirection.Descending, SortOptimisations.ComparesImmutableValuesOnly)
                .Transform(p => new TagViewModel() { Value = p.Key, IsSelected = _excludedTagValues.Contains(p.Key) })
            ;
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
            _trackCache.AddOrUpdate(result.Results.Select(p => p.ToViewModel()));

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

            var tracksByTags = new ConcurrentDictionary<string, ConcurrentStack<TrackViewModel>>();
            var tracksByGenres = new ConcurrentDictionary<string, ConcurrentStack<TrackViewModel>>();
            var tracksByArtist = new ConcurrentDictionary<Guid, ConcurrentStack<TrackViewModel>>();
            var tracksByRelease = new ConcurrentDictionary<Guid, ConcurrentStack<TrackViewModel>>();
            var artists = new ConcurrentDictionary<Guid, ArtistViewModel>();
            var releases = new ConcurrentDictionary<Guid, ReleaseViewModel>();
            var tracks = new ConcurrentBag<TrackViewModel>();
            var tasks = new List<Task>();

            foreach (var batch in requests.Batch(8)) // ToDo make configurable
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
                            // tags
                            track.Tags = track.Tags?.Select(p => p.Trim().ToLowerInvariant()).ToArray();

                            // track
                            var trackViewModel = track.ToViewModel();
                            tracks.Add(trackViewModel);
                            if (trackViewModel.Tags.Count > 0)
                            {
                                foreach (var tag in trackViewModel.Tags)
                                {
                                    tracksByTags.AddOrUpdate(tag, new ConcurrentStack<TrackViewModel>(new[] { trackViewModel }), (_, stack) =>
                                    {
                                        stack.Push(trackViewModel);
                                        return stack;
                                    });
                                }
                            }

                            // release
                            var rleaseViewModel = releases.AddOrUpdate(track.Release.Id, trackViewModel.Release, (_, old) => old);
                            tracksByRelease.AddOrUpdate(track.Release.Id, new ConcurrentStack<TrackViewModel>(new[] { trackViewModel }), (_, stack) =>
                            {
                                stack.Push(trackViewModel);
                                return stack;
                            });

                            // genres
                            tracksByGenres.AddOrUpdate(track.GenrePrimary, new ConcurrentStack<TrackViewModel>(new[] { trackViewModel }), (_, stack) =>
                            {
                                stack.Push(trackViewModel);
                                return stack;
                            });

                            tracksByGenres.AddOrUpdate(track.GenreSecondary, new ConcurrentStack<TrackViewModel>(new[] { trackViewModel }), (_, stack) =>
                            {
                                stack.Push(trackViewModel);
                                return stack;
                            });

                            // artists
                            if (track.Artists?.Length > 0)
                            {
                                for (var i = 0; i < track.Artists.Length; i++)
                                {
                                    var artist = track.Artists[i];
                                    tracksByArtist.AddOrUpdate(artist.Id, new ConcurrentStack<TrackViewModel>(new[] { trackViewModel }), (_, stack) =>
                                    {
                                        stack.Push(trackViewModel);
                                        return stack;
                                    });

                                    artists.AddOrUpdate(artist.Id, artist.ToViewModel(), (_, old) => old);
                                }
                            }

                            trackViewModel.Release = rleaseViewModel;
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            _trackCache.AddOrUpdate(tracks);
            _tagCache.AddOrUpdate(tracksByTags.Select(p => new KeyValuePair<string, List<TrackViewModel>>(p.Key, p.Value.ToList())));
            _genreCache.AddOrUpdate(tracksByGenres.Select(p => new KeyValuePair<string, List<TrackViewModel>>(p.Key, p.Value.ToList())));

            foreach (var artist in artists)
            {
                var artistTracks = tracksByArtist[artist.Key].OrderByDescending(p => p.ReleaseDate).ToArray();

                artist.Value.Tracks.AddRange(artistTracks);
                artist.Value.LatestReleaseDate = artistTracks.FirstOrDefault()?.ReleaseDate ?? DateTime.MinValue;
                artist.Value.LatestReleaseCount = artistTracks.Count(p => p.ReleaseDate == artist.Value.LatestReleaseDate);
            }

            _artistCache.AddOrUpdate(artists.Values);

            foreach (var release in releases)
            {
                var releaseTracks = tracksByRelease[release.Key];
                release.Value.Tracks.AddRange(releaseTracks);
            }

            _releaseCache.AddOrUpdate(releases.Values);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _trackCache.Dispose();
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

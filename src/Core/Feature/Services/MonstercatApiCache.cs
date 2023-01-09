using Microsoft.IO;
using Refit;
using Serilog;
using SoftThorn.MonstercatNet;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class MonstercatApiCache : IMonstercatApi
    {
        private readonly IMonstercatApi _monstercatApi;
        private readonly MonstercatContentStorageService _monstercatContentStorageService;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;
        private readonly SettingsService _settingsService;
        private readonly ILogger _logger;
        private readonly JsonSerializerOptions _serializerOptions;

        public MonstercatApiCache(
            IMonstercatApi monstercatApi,
            MonstercatContentStorageService monstercatContentStorageService,
            RecyclableMemoryStreamManager memoryStreamManager,
            SettingsService settingsService,
            ILogger logger)
        {
            _monstercatApi = monstercatApi ?? throw new ArgumentNullException(nameof(monstercatApi));
            _monstercatContentStorageService = monstercatContentStorageService ?? throw new ArgumentNullException(nameof(monstercatContentStorageService));
            _memoryStreamManager = memoryStreamManager ?? throw new ArgumentNullException(nameof(memoryStreamManager));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger?.ForContext<MonstercatApiCache>() ?? throw new ArgumentNullException(nameof(logger));

            _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General)
            {
                AllowTrailingCommas = false,
                WriteIndented = false,
            };
        }

        public Task<CreatePlaylistResult> CreatePlaylist(PlaylistCreateRequest request, CancellationToken token = default)
        {
            return _monstercatApi.CreatePlaylist(request, token);
        }

        public Task DeletePlaylist([Query] Guid playlistId, CancellationToken token = default)
        {
            return _monstercatApi.DeletePlaylist(playlistId, token);
        }

        public Task<HttpContent> DownloadTrack([Query] TrackDownloadRequest request, CancellationToken token = default)
        {
            return _monstercatApi.DownloadTrack(request, token);
        }

        public Task<GetPlaylistResult> GetPlaylist([Query] Guid playlistId, CancellationToken token = default)
        {
            return _monstercatApi.GetPlaylist(playlistId, token);
        }

        public Task<ReleaseResult> GetRelease([Query] string catalogId, CancellationToken token = default)
        {
            return _monstercatApi.GetRelease(catalogId, token);
        }

        public Task<ReleaseBrowseResult> GetReleases([Query] ReleaseBrowseRequest request, CancellationToken token = default)
        {
            return _monstercatApi.GetReleases(request, token);
        }

        public Task<Self> GetSelf(CancellationToken token = default)
        {
            return _monstercatApi.GetSelf(token);
        }

        public Task<SelfPlaylistsResult> GetSelfPlaylists(CancellationToken token = default)
        {
            return _monstercatApi.GetSelfPlaylists(token);
        }

        public Task<TrackFilters> GetTrackSearchFilters(CancellationToken token = default)
        {
            return _monstercatApi.GetTrackSearchFilters(token);
        }

        public Task Login([Body(BodySerializationMethod.Serialized)] ApiCredentials credentials, CancellationToken token = default)
        {
            return _monstercatApi.Login(credentials, token);
        }

        public Task Login(string twoFactorAuthToken, CancellationToken token = default)
        {
            return _monstercatApi.Login(twoFactorAuthToken, token);
        }

        public Task Logout(CancellationToken token = default)
        {
            return _monstercatApi.Logout(token);
        }

        public Task PlaylistAddTrack(Guid playlistId, PlaylistAddTrackRequest request, CancellationToken token = default)
        {
            return _monstercatApi.PlaylistAddTrack(playlistId, request, token);
        }

        public Task PlaylistDeleteTrack(Guid playlistId, PlaylistDeleteTrackRequest request, CancellationToken token = default)
        {
            return _monstercatApi.PlaylistDeleteTrack(playlistId, request, token);
        }

        public Task Resend(string twoFactorAuthToken, CancellationToken token = default)
        {
            return _monstercatApi.Resend(twoFactorAuthToken, token);
        }

        public Task<HttpContent> StreamTrack([Query] TrackStreamRequest request, CancellationToken token = default)
        {
            return _monstercatApi.StreamTrack(request, token);
        }

        public Task<UpdatePlaylistResult> UpdatePlaylist(UpdatePlaylistRequest request, CancellationToken token = default)
        {
            return _monstercatApi.UpdatePlaylist(request, token);
        }

        public async Task<TrackSearchResult> SearchTracks([Query(CollectionFormat = CollectionFormat.Csv)] TrackSearchRequest request, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(_settingsService.MonstercatContentFileStorageDirectoryPath))
            {
                _logger.Verbose("[CACHEMISS] for {Url}", "SearchTracks");
                return await _monstercatApi.SearchTracks(request, token);
            }

            var md5 = await GetMd5HashFor(request, token);

            var filePath = Path.Combine("SoftThorn", "SoftThorn.Monstercat.Browser.Wpf", _settingsService.MonstercatContentFileStorageDirectoryPath, md5);
            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Exists)
            {
                var delta = DateTime.Now - fileInfo.LastWriteTime;
                if (delta > TimeSpan.FromMinutes(5))
                {
                    return await Fetch(fileInfo);
                }
                else
                {
                    return await _monstercatContentStorageService.Read(filePath, token);
                }
            }
            else
            {
                return await Fetch(fileInfo);
            }

            async Task<TrackSearchResult> Fetch(FileInfo fileInfo)
            {
                _logger.Verbose("[CACHEMISS] for {Url}", "SearchTracks");
                var results = await _monstercatApi.SearchTracks(request, token);
                await _monstercatContentStorageService.Write(fileInfo.FullName, results, fileInfo.Exists, token);

                return results;
            }
        }

        private async Task<string> GetMd5HashFor(TrackSearchRequest request, CancellationToken token)
        {
            using (var resultStream = _memoryStreamManager.GetStream())
            {
                await JsonSerializer.SerializeAsync(resultStream, request, _serializerOptions, token);
                resultStream.Seek(0, SeekOrigin.Begin);
                return await resultStream.CalculateMd5(token);
            }
        }
    }
}

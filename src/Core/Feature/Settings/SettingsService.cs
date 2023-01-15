using Akavache;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class SettingsService
    {
        private readonly ISecureBlobCache _secureBlobCache;
        private readonly IBlobCache _blobCache;
        private readonly IConfiguration _configuration;
        private readonly IMessenger _messenger;

        // dashboard settings

        /// <summary>
        /// tags that wont be displayed on the dashboard
        /// </summary>
        public string[] ExcludedTags { get; set; } = Array.Empty<string>();

        public int ArtistsCount { get; set; }

        public int GenresCount { get; set; }

        public int ReleasesCount { get; set; }

        public int TagsCount { get; set; }

        public int TracksCount { get; set; }

        // download settings

        /// <summary>
        /// where to download tracks to
        /// </summary>
        public string? DownloadTracksPath { get; set; }

        /// <summary>
        /// where to store all the images for the displayed tracks
        /// </summary>
        public string? DownloadImagesPath { get; set; }

        public FileFormat DownloadFileFormat { get; set; }

        public int ParallelDownloads { get; set; }

        // login settings
        /// <summary>
        /// E-Mail for your monstercat account
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Your monstercat password
        /// </summary>
        public string? Password { get; set; }

        public string? MonstercatContentFileStorageDirectoryPath { get; set; }

        public int Volume { get; set; }

        public SettingsService(
            ISecureBlobCache secureBlobCache,
            IBlobCache blobCache,
            IConfiguration configuration,
            IMessenger messenger)
        {
            _secureBlobCache = secureBlobCache ?? throw new ArgumentNullException(nameof(secureBlobCache));
            _blobCache = blobCache ?? throw new ArgumentNullException(nameof(blobCache));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        private async Task<MonstercatCredentialsModel> LoadCredentials()
        {
            try
            {
                return await _secureBlobCache.GetObject<MonstercatCredentialsModel>(nameof(MonstercatCredentialsModel));
            }
            catch (KeyNotFoundException)
            {
                return new MonstercatCredentialsModel();
            }
        }

        private async Task SaveCredentials(MonstercatCredentialsModel credentials)
        {
            await _secureBlobCache.InsertObject(nameof(MonstercatCredentialsModel), credentials);
        }

        private async Task<SettingsModel> LoadSettings()
        {
            try
            {
                return await _blobCache.GetObject<SettingsModel>(nameof(SettingsModel));
            }
            catch (KeyNotFoundException)
            {
                return new SettingsModel()
                {
                    ArtistsCount = 10,
                    TagsCount = 10,
                    GenresCount = 10,
                    ReleasesCount = 10,
                    TracksCount = 10,
                    Volume = 50,
                    DownloadImagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache), "SoftThorn.Monstercat.Browser.Wpf"),
                    DownloadTracksPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SoftThorn.Monstercat.Browser.Wpf"),
                    MonstercatContentFileStorageDirectoryPath = Path.Combine(GetCommonApplicationDataPath(), "RequestCache"),
                    ParallelDownloads = Math.Min(4, Environment.ProcessorCount),
                    DownloadFileFormat = FileFormat.flac,
                    ExcludedTags = new[]
                    {
                        "and",
                        "beat",
                        "beatport",
                        "big",
                        "blue",
                        "bro",
                        "build",
                        "christmas",
                        "dj",
                        "djmixma",
                        "djmixmatcher",
                        "dspexclusive",
                        "edmspotlight",
                        "featured",
                        "featured sync",
                        "festive",
                        "flume",
                        "fun",
                        "glitched-mp3-320",
                        "good",
                        "green",
                        "grey",
                        "halloween",
                        "hands",
                        "hip",
                        "hop",
                        "instinct",
                        "instinctvol5",
                        "intro",
                        "marshmello",
                        "mix",
                        "monstercat",
                        "moombahcore",
                        "moombahton",
                        "nocontentid",
                        "nu",
                        "odesza",
                        "or",
                        "orange",
                        "out",
                        "pink",
                        "pop / top40",
                        "riddim",
                        "rlmesa",
                        "rlnonprimary",
                        "rocketleague",
                        "shorts",
                        "silk-migration",
                        "silkinitialbulkimport",
                        "tempnocontentid",
                        "titties",
                        "uncaged",
                        "up",
                        "yellow",
                    }
                };
            }
        }

        private async Task SaveSettings(SettingsModel settings)
        {
            await _blobCache.InsertObject(nameof(SettingsModel), settings);
        }

        public async Task ResetSettings()
        {
            await _blobCache.Invalidate(nameof(SettingsModel));
        }

        public async Task ResetCredentials()
        {
            await _secureBlobCache.Invalidate(nameof(MonstercatCredentialsModel));
        }

        public async Task Load()
        {
            var settings = await LoadSettings();
            CreateDirectory(settings.DownloadTracksPath);
            CreateDirectory(settings.DownloadImagesPath);

            DownloadTracksPath = settings.DownloadTracksPath;
            DownloadImagesPath = settings.DownloadImagesPath;
            DownloadFileFormat = settings.DownloadFileFormat;
            ParallelDownloads = settings.ParallelDownloads;

            ExcludedTags = settings.ExcludedTags;

            ArtistsCount = settings.ArtistsCount <= 0 ? 10 : settings.ArtistsCount;
            GenresCount = settings.GenresCount <= 0 ? 10 : settings.GenresCount;
            ReleasesCount = settings.ReleasesCount <= 0 ? 10 : settings.ReleasesCount;
            TagsCount = settings.TagsCount <= 0 ? 10 : settings.TagsCount;
            TracksCount = settings.TracksCount <= 0 ? 10 : settings.TracksCount;
            Volume = settings.Volume <= 0
                            ? 50
                            : settings.Volume > 100
                                ? 50
                                : settings.Volume;

            MonstercatContentFileStorageDirectoryPath = settings.MonstercatContentFileStorageDirectoryPath ?? Path.Combine(GetCommonApplicationDataPath(), "RequestCache");
            CreateDirectory(MonstercatContentFileStorageDirectoryPath);

            var sectionName = typeof(ApiCredentials).Name;
            var section = _configuration.GetSection(sectionName);
            if (section is not null)
            {
                var credentials = new ApiCredentials();
                section.Bind(credentials);

                Email = credentials.Email;
                Password = credentials.Password;
            }

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                var credentials = await LoadCredentials();
                Email = credentials.Email;
                Password = credentials.Password;
            }

            _messenger.Send(new SettingsChangedMessage(CreateModel(this)));

            static void CreateDirectory(string? directoryPath)
            {
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    return;
                }

                Directory.CreateDirectory(directoryPath);
            }
        }

        public async Task Save()
        {
            var settings = CreateModel(this);

            await SaveSettings(settings);

            var credentials = new MonstercatCredentialsModel
            {
                Email = Email,
                Password = Password
            };

            await SaveCredentials(credentials);

            _messenger.Send(new SettingsChangedMessage(settings));
        }

        private static string GetCommonApplicationDataPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SoftThorn", "SoftThorn.Monstercat.Browser.Wpf");
        }

        private static SettingsModel CreateModel(SettingsService service)
        {
            return new SettingsModel
            {
                DownloadTracksPath = service.DownloadTracksPath,
                DownloadImagesPath = service.DownloadImagesPath,
                DownloadFileFormat = service.DownloadFileFormat,
                ParallelDownloads = service.ParallelDownloads,

                ExcludedTags = service.ExcludedTags,

                ArtistsCount = service.ArtistsCount,
                GenresCount = service.GenresCount,
                ReleasesCount = service.ReleasesCount,
                TagsCount = service.TagsCount,
                TracksCount = service.TracksCount,

                MonstercatContentFileStorageDirectoryPath = service.MonstercatContentFileStorageDirectoryPath ?? Path.Combine(GetCommonApplicationDataPath(), "RequestCache"),
                Volume = service.Volume,
            };
        }
    }
}

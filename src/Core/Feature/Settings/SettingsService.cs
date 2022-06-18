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

        // login settings
        /// <summary>
        /// E-Mail for your monstercat account
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Your monstercat password
        /// </summary>
        public string? Password { get; set; }

        public SettingsService(ISecureBlobCache secureBlobCache, IBlobCache blobCache, IConfiguration configuration, IMessenger messenger)
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
                    DownloadImagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache), "SoftThorn.Monstercat.Browser.Wpf"),
                    DownloadTracksPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SoftThorn.Monstercat.Browser.Wpf"),
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

        private async Task SaveSettings(SettingsModel credentials)
        {
            await _blobCache.InsertObject(nameof(SettingsModel), credentials);
        }

        public async Task Load()
        {
            var settings = await LoadSettings();
            DownloadTracksPath = settings.DownloadTracksPath;
            DownloadImagesPath = settings.DownloadImagesPath;
            DownloadFileFormat = settings.DownloadFileFormat;

            ExcludedTags = settings.ExcludedTags;

            ArtistsCount = settings.ArtistsCount;
            GenresCount = settings.GenresCount;
            ReleasesCount = settings.ReleasesCount;
            TagsCount = settings.TagsCount;

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

            _messenger.Send(new SettingsChangedMessage()
            {
                Settings = settings,
            });
        }

        public async Task Save()
        {
            var settings = new SettingsModel
            {
                DownloadTracksPath = DownloadTracksPath,
                DownloadImagesPath = DownloadImagesPath,
                DownloadFileFormat = DownloadFileFormat,

                ExcludedTags = ExcludedTags,

                ArtistsCount = ArtistsCount,
                GenresCount = GenresCount,
                ReleasesCount = ReleasesCount,
                TagsCount = TagsCount
            };

            await SaveSettings(settings);

            var credentials = new MonstercatCredentialsModel
            {
                Email = Email,
                Password = Password
            };

            await SaveCredentials(credentials);

            _messenger.Send(new SettingsChangedMessage()
            {
                Settings = settings,
            });
        }
    }
}

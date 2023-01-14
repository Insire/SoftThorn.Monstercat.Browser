using FluentAssertions;
using Gress;
using Microsoft.Extensions.ObjectPool;
using Moq;
using Serilog;
using Serilog.Sinks.TestCorrelator;
using SoftThorn.Monstercat.Browser.Core;
using SoftThorn.MonstercatNet;
using Xunit.Abstractions;

namespace SoftThorn.Monstercat.UnitTests
{
    public sealed class DownloadServiceTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly ObjectPoolProvider _objectPoolProvider;

        public DownloadServiceTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _objectPoolProvider = new DefaultObjectPoolProvider();
        }

        public static IEnumerable<object[]> GetDownloadNumbers()
        {
            // batch size 1
            yield return new object[] { Enumerable.Range(0, 0), Enumerable.Range(0, 0), 1 }; // 0 downloads
            yield return new object[] { Enumerable.Range(0, 1), Enumerable.Range(1, 2), 1 }; // 1 download
            yield return new object[] { Enumerable.Range(0, 0), Enumerable.Range(0, 10), 1 }; // 10 downloads

            // batch size 3
            yield return new object[] { Enumerable.Range(0, 0), Enumerable.Range(0, 0), 3 }; // 0 downloads
            yield return new object[] { Enumerable.Range(0, 1), Enumerable.Range(1, 2), 3 }; // 1 download
            yield return new object[] { Enumerable.Range(0, 0), Enumerable.Range(0, 10), 3 }; // 10 downloads
        }

        [Theory]
        [MemberData(nameof(GetDownloadNumbers))]
        public async Task Download_Should_Work_As_Expected(IEnumerable<int> existingFiles, IEnumerable<int> filesToDownload, int batchSize)
        {
            const string DOWNLOAD_PATH = "\\\\some\\directory\\path";

            // Arrange
            var filesToDownloadCount = filesToDownload.Count();
            var percentages = new List<Percentage>();
            var objectPool = _objectPoolProvider.CreateStringBuilderPool();

            var toasts = new Mock<IToastService>();
            var progress = new Mock<IProgress<Percentage>>();
            progress
                .Setup(p => p.Report(It.IsAny<Percentage>()))
                .Callback<Percentage>(p => percentages.Add(p));

            var api = new Mock<IMonstercatApi>();
            api
                .Setup(p => p.DownloadTrack(It.IsAny<TrackDownloadRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<HttpContent>(new StreamContent(new MemoryStream())));

            var fileSystem = new Mock<IFileSystemService>();
            fileSystem
                .Setup(p => p.FileOpen(It.IsAny<string>()))
                .Returns(() => new MemoryStream());

            fileSystem
                .Setup(p => p.DirectoryGetFiles(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(existingFiles
                    .Select(p => Path.Combine(DOWNLOAD_PATH, $"File {p}{FileFormat.flac.GetFileExtension()}"))
                    .ToArray());

            // Act
            using (TestCorrelator.CreateContext())
            {
                using var log = new LoggerConfiguration()
                  .WriteTo.TestOutput(_testOutputHelper)
                  .WriteTo.TestCorrelator()
                  .CreateLogger();

                var sut = new DownloadService(fileSystem.Object, log, api.Object, toasts.Object, objectPool);

                await sut.Download(new DownloadOptions()
                {
                    DownloadFileFormat = FileFormat.flac,
                    DownloadTracksPath = DOWNLOAD_PATH,
                    ParallelDownloads = batchSize,
                    Tracks = filesToDownload
                                    .Select(p => new TrackViewModel()
                                    {
                                        Id = Guid.NewGuid(),
                                        CatalogId = Guid.NewGuid().ToString(),
                                        Key = Guid.NewGuid().ToString(),
                                        Title = $"Title {p}",
                                        ArtistsTitle = $"Artist {p}",
                                        Brand = $"Brand {p}",
                                        GenrePrimary = $"GenrePrimary {p}",
                                        GenreSecondary = $"GenreSecondary {p}",
                                        FileName = $"File {p}",
                                        DebutDate = DateTime.Now,
                                        ReleaseDate = DateTime.Now,
                                        Downloadable = true,
                                        InEarlyAccess = false,
                                        Streamable = true,
                                        Tags = new System.Collections.ObjectModel.ObservableCollection<string>(),
                                        Type = string.Empty,
                                        Version = string.Empty,
                                        Release = new ReleaseViewModel()
                                        {
                                            Id = Guid.NewGuid(),
                                            Title = $"Title {p}",
                                            ArtistsTitle = $"Artist {p}",
                                            CatalogId = Guid.NewGuid().ToString(),
                                            Version = string.Empty,
                                            ReleaseDate = DateTime.Now,
                                            Type = string.Empty,
                                            Description = string.Empty,
                                            Tags = Array.Empty<string>(),
                                            Upc = string.Empty,
                                        },
                                    })
                                    .ToList()
                }, progress.Object, CancellationToken.None);

                // Assert
                api.Verify(p => p.DownloadTrack(It.IsAny<TrackDownloadRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(filesToDownloadCount));

                var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
                logEvents
                    .Where(p => p.MessageTemplate.Text == "Downloading {TrackId} {ReleaseId} to {FilePath}")
                    .Should()
                    .HaveCount(filesToDownloadCount);

                percentages.Should().HaveCount(filesToDownloadCount);
            }
        }
    }
}

using CommunityToolkit.Mvvm.Messaging;
using EmailValidation;
using Gress;
using SoftThorn.Monstercat.Browser.Core;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Reactive.Linq;

namespace SoftThorn.Monstercat.Browser.Cli.Commands
{
    internal sealed class DownloadCommand : AsyncCommand<DownloadCommand.Settings>
    {
        private readonly ITrackRepository _trackRepository;
        private readonly IMessenger _messenger;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly ShellViewModel _shellViewModel;
        private readonly LoginViewModel _loginViewModel;
        private readonly DispatcherProgressFactory<Percentage> _progressFactory;
        private readonly DownloadViewModel _downloadViewModel;

        public DownloadCommand(
            ITrackRepository trackRepository,
            IMessenger messenger,
            SettingsViewModel settingsViewModel,
            ShellViewModel shellViewModel,
            LoginViewModel loginViewModel,
            DispatcherProgressFactory<Percentage> progressFactory,
            DownloadViewModel downloadViewModel)
        {
            _trackRepository = trackRepository;
            _messenger = messenger;
            _settingsViewModel = settingsViewModel;
            _shellViewModel = shellViewModel;
            _loginViewModel = loginViewModel;
            _progressFactory = progressFactory;
            _downloadViewModel = downloadViewModel;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Line)
                .StartAsync("Preparing download...", async ctx =>
                {
                    ctx.Status = "Loading settings...";
                    await _settingsViewModel.Load();

                    ctx.Status = "Trying login...";
                    _loginViewModel.Email = settings.Email;
                    _loginViewModel.Password = settings.Password;

                    await _loginViewModel.TryLogin(null, CancellationToken.None);

                    var fileFormat = settings.DownloadFileFormat switch
                    {
                        1 => MonstercatNet.FileFormat.flac,
                        2 => MonstercatNet.FileFormat.mp3,
                        3 => MonstercatNet.FileFormat.wav,
                        _ => throw new NotImplementedException(),
                    };

                    _messenger.Send(new SettingsChangedMessage(new SettingsModel()
                    {
                        ArtistsCount = 1,
                        TagsCount = 1,
                        GenresCount = 1,
                        ReleasesCount = 1,
                        DownloadFileFormat = fileFormat,
                        DownloadTracksPath = settings.DownloadLocation,
                        ParallelDownloads = settings.ConcurrentDownloads,
                    }));
                });

            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var tracksToDownload = new List<TrackViewModel>();

                    var task1 = ctx.AddTask("[green]Fetching tracks[/]", true, 100);
                    var task2 = ctx.AddTask("[green]Downloading tracks[/]", true, 100);

                    using (_progressFactory.Create((percentage) => task1.Value = percentage.Value))
                    {
                        await _shellViewModel.Refresh();

                        var changeSet = await _trackRepository.ConnectTracks().FirstOrDefaultAsync();

                        var tracks = new List<TrackViewModel>();

                        foreach (var track in changeSet)
                        {
                            tracks.Add(track.Current);
                        }

                        tracksToDownload = tracks;
                        task1.Value = 100;
                    }

                    using (_progressFactory.Create((percentage) => task2.Value = percentage.Value))
                    {
                        await _downloadViewModel.Download(tracksToDownload, CancellationToken.None);
                        task2.Value = 100;
                    }
                });

            return 0;
        }

        public class Settings : CommandSettings
        {
            [Description("The E-Mail of your Monstercat-Account with an active Gold subscription")]
            [CommandArgument(0, "<E-Mail>")]
            [CommandOption("-e|--email")]
            public string Email { get; init; } = string.Empty;

            [Description("The Password to your Monstercat-Account with an active Gold subscription")]
            [CommandArgument(1, "<Password>")]
            [CommandOption("-p|--password")]
            public string Password { get; init; } = string.Empty;

            [Description("Where you want to store the downloaded tracks")]
            [CommandArgument(2, "<DownloadLocation>")]
            [CommandOption("-d|--downloadlocation")]
            public string DownloadLocation { get; init; } = string.Empty;

            [Description("The file format and file extension you want the files to have (1 = flac, 2 = mp3, 3 = wav)")]
            [CommandArgument(3, "[DownloadFileFormat]")]
            [CommandOption("-f|--downloadfileformat")]
            [DefaultValue(1)]
            public int DownloadFileFormat { get; init; }

            [Description("How many files should be downloaded concurrently")]
            [CommandArgument(4, "[ConcurrentDownloads]")]
            [CommandOption("-c|--concurrentdownloads", IsHidden = true)]
            [DefaultValue(4)]
            public int ConcurrentDownloads { get; init; }

            public override ValidationResult Validate()
            {
                var result = !EmailValidator.Validate(Email, allowTopLevelDomains: true, allowInternational: true)
                    ? ValidationResult.Error("The provided E-Mail isnt valid.")
                    : ValidationResult.Success();
                if (!result.Successful)
                {
                    return result;
                }

                result = string.IsNullOrWhiteSpace(Password)
                    ? ValidationResult.Error("The Password is required")
                    : ValidationResult.Success();

                if (!result.Successful)
                {
                    return result;
                }

                result = string.IsNullOrWhiteSpace(DownloadLocation)
                    ? ValidationResult.Error("The DownloadLocation is required")
                    : ValidationResult.Success();

                if (!result.Successful)
                {
                    return result;
                }

                return !Directory.Exists(DownloadLocation)
                    ? ValidationResult.Error("The provided DownloadLocation is does not point to an existing directory.")
                    : ValidationResult.Success();
            }
        }
    }
}

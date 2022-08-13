using SoftThorn.Monstercat.Browser.Cli.Commands;
using SoftThorn.Monstercat.Browser.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SoftThorn.Monstercat.Browser.Cli
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += OnUnhandledException;

            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            Akavache.Registrations.Start("SoftThorn.Monstercat.Browser.Cli");

            // startup
            var container = CompositionRoot.Get();

            var app = new CommandApp<DownloadCommand>(new TypeRegistrar(container));

            try
            {
                return app.Run(args);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                return -99;
            }
            finally
            {
                // shutdown
                Akavache.BlobCache.Shutdown().Wait();

                Serilog.Log.CloseAndFlush();

                container?.Dispose();
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                Serilog.Log.Error(exception, "AppDomain.CurrentDomain.UnhandledException");
            }
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Serilog.Log.Error(e.Exception, "TaskScheduler.UnobservedTaskException");
        }
    }
}

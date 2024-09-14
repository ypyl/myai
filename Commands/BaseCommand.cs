
using Serilog;
using Spectre.Console.Cli;

internal abstract class BaseCommand<T> : AsyncCommand<T> where T : CommandSettings
{
    protected readonly Config _config = new ();

    protected ILogger Logger { get; }

    protected BaseCommand()
    {
        Logger = CreateLogger();
    }

    private ILogger CreateLogger()
    {
        var debug = _config.GetBoolValue("$.debug");
        var logDir = _config.GetStringValue("$.log_dir");
        if (debug && !string.IsNullOrEmpty(logDir) && Directory.Exists(logDir))
        {
            // Generate a unique filename using a timestamp
            var logFileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var logFilePath = Path.Combine(logDir, logFileName);

            // Initialize the logger with the constructed file path
            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(logFilePath, retainedFileCountLimit: 5)
                .CreateLogger();
        }
        return Serilog.Core.Logger.None;
    }
}

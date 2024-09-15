using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;
internal abstract class BaseCommand<T> : AsyncCommand<T> where T : CommandSettings
{
    protected readonly Config _config = new();

    protected ExternalProcessPlugin ExternalProcessPlugin { get; }
    protected ILogger Logger { get; }
    protected BaseCommand()
    {
        Logger = CreateLogger();
        ExternalProcessPlugin = new ExternalProcessPlugin(_config.GetStringValue("$.processName"), _config.GetIntValue("$.pid"));
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

    // TODO refactor

    protected async Task<List<string>> ExternalContext(Dictionary<string, string> allFiles, string targetFile)
    {
        AnsiConsole.MarkupLine("[fuchsia]Extracting external types from the target code.[/]");
        var externalTypes = new CsNonStandardTypeExtractorPlugin().ExtractNonStandardTypes(targetFile);
        var result = new List<string>();
        foreach (var path in allFiles.Where(x => externalTypes.Contains(x.Key)).Select(x => x.Value))
        {
            var content = await new FileIOPlugin().ReadAsync(path);
            AnsiConsole.MarkupLine("[green]External context[/]: [navy]{0}[/]", path);
            result.Add(content);
        }
        return result;
    }

    protected async Task<List<string>> ExternalTypesFromInstructionContext(string promptTypesFromInstructions, Dictionary<string, string> allFiles, string targetFileContent, ILogger logger)
    {
        AnsiConsole.MarkupLine("[fuchsia]Getting external types from instructions.[/]");
        var completionService = new CompletionService(_config).CreateChatCompletionService();

        var conversation = new Conversation(_config, completionService, logger);

        var userMessage = await new PromptFactory(logger).RenderPrompt(promptTypesFromInstructions,
            new Dictionary<string, object?> { ["csharp_code"] = targetFileContent });

        var answer = await conversation.Say(userMessage);
        var typesFromInstructions = answer.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        foreach (var path in allFiles.Where(x => typesFromInstructions.Contains(x.Key)).Select(x => x.Value))
        {
            var content = await new FileIOPlugin().ReadAsync(path);
            AnsiConsole.MarkupLine("[green]External context[/]: [navy]{0}[/]", path);
            result.Add(content);
        }
        return result;
    }
}

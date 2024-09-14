
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Core;
using Spectre.Console;
using Spectre.Console.Cli;

internal sealed class CodeCommand : AsyncCommand<CodeCommand.Settings>
{
    private string PromptMain => _config.GetStringValue("$.code.main");
    private string PromptTypesFromInstructions => _config.GetStringValue("$.code.types_from_code");
    private string PromptRegenerate => _config.GetStringValue("$.code.regenerate");
    private readonly Config _config = new ();

    public sealed class Settings : CommandSettings
    {

    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var logger = CreateLogger();
        var targetFileName = new ExternalProcessPlugin().VSCodeTargetFileName();

        var allFiles = new FileFinderPlugin(_config.GetStringValue("$.working_dir"), logger).FindCsFiles();
        if (!allFiles.TryGetValue(Path.GetFileNameWithoutExtension(targetFileName), out var targetFilePath))
        {
            AnsiConsole.MarkupLine("[red]Not able to find {0} in workding directory.[/]", targetFileName);
            return 1;
        }
        var fileContent = await new FileIOPlugin().ReadAsync(targetFilePath);

        var additionalFromInstruction = await ExternalTypesFromInstructionContext(allFiles, fileContent, logger);

        var additional = await ExternalContext(allFiles, fileContent);

        List<string> additionalFileContents = [.. additionalFromInstruction, .. additional];
        var filtered = additionalFileContents.Distinct();

        var userMessage = await new PromptFactory(logger).RenderPrompt(PromptMain,
            new Dictionary<string, object?> { ["csharp_code"] = fileContent, ["csharp_additional_code"] = string.Join("\n\n", filtered) });

        var completionService = new CompletionService(_config).CreateChatCompletionService();

        var conversation = new Conversation(_config, completionService, logger);

        var answer = await conversation.Say(userMessage);
        const string Prefix = "```csharp";
        const string Postfix = "```";

        var regenerate = true;
        while (regenerate)
        {
            while (!answer.StartsWith(Prefix) || !answer.EndsWith(Postfix))
            {
                // TODO loop protection
                answer = await conversation.Say(string.Format(PromptRegenerate, Prefix, Postfix));
            }
            var codeOnly = answer[Prefix.Length..^Postfix.Length];

            await new FileIOPlugin().WriteAsync(targetFilePath, codeOnly.TrimStart());

            regenerate = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Do you like [green]created code[/]?")
                    .AddChoices(["Yes", "No"])) == "No";

            if (!regenerate) break;

            var userComment = AnsiConsole.Prompt(new TextPrompt<string>("[red]What is the issue with generated code?[/]"));

            answer = await conversation.Say(userComment);
        }

        return 0;
    }

    private async Task<List<string>> ExternalTypesFromInstructionContext(Dictionary<string, string> allFiles, string targetFileContent, ILogger logger)
    {
        var completionService = new CompletionService(_config).CreateChatCompletionService();

        var conversation = new Conversation(_config, completionService, logger);

        var userMessage = await new PromptFactory(logger).RenderPrompt(PromptTypesFromInstructions,
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

    private async Task<List<string>> ExternalContext(Dictionary<string, string> allFiles, string targetFile)
    {
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
        return Logger.None;
    }
}

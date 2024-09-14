using System.Diagnostics.CodeAnalysis;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

internal sealed class SnippetCommand : BaseCommand<SnippetCommand.Settings>
{
    private string PromptMain => _config.GetStringValue("$.snippet.main");
    private string PromptTypesFromInstructions => _config.GetStringValue("$.snippet.types_from_code");
    private string PromptRegenerate => _config.GetStringValue("$.snippet.regenerate");

    public sealed class Settings : CommandSettings
    {

    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var targetFileName = new ExternalProcessPlugin().VSCodeTargetFileName();

        var allFiles = new FileFinderPlugin(_config.GetStringValue("$.working_dir"), Logger).FindCsFiles();
        if (!allFiles.TryGetValue(Path.GetFileNameWithoutExtension(targetFileName), out var targetFilePath))
        {
            AnsiConsole.MarkupLine("[red]Not able to find {0} in workding directory.[/]", targetFileName);
            return 1;
        }
        var fileContent = await new FileIOPlugin().ReadAsync(targetFilePath);

        var lines = fileContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        var linesWithInstructions = lines.Where(x => x.TrimStart().StartsWith("// @myai")).ToList();
        if (linesWithInstructions.Count != 1)
        {
            AnsiConsole.MarkupLine("[red]File {0} contains two or more instructions started from // @myai[/]", targetFilePath);
            return 2;
        }

        var instructionIndex = lines.FindIndex(x => x == linesWithInstructions[0]);
        var tempIndex = instructionIndex + 1;
        while (tempIndex < lines.Count && lines[tempIndex].TrimStart().StartsWith("//"))
        {
            tempIndex += 1;
        }

        var start = lines.Take(instructionIndex).ToList();

        var end = lines.Skip(tempIndex).ToList();

        var additionalFromInstruction = await ExternalTypesFromInstructionContext(allFiles, fileContent, Logger);

        var additional = await ExternalContext(allFiles, fileContent);

        List<string> additionalFileContents = [.. additionalFromInstruction, .. additional];
        var filtered = additionalFileContents.Distinct();

        var userMessage = await new PromptFactory(Logger).RenderPrompt(PromptMain,
            new Dictionary<string, object?> { ["csharp_code"] = fileContent, ["csharp_additional_code"] = string.Join("\n\n", filtered) });

        var completionService = new CompletionService(_config).CreateChatCompletionService();

        var conversation = new Conversation(_config, completionService, Logger);

        var answer = await conversation.Say(userMessage);
        const string Prefix = "```csharp";
        const string Postfix = "```";

        var regenerate = true;
        while (regenerate)
        {
            while (!answer.StartsWith(Prefix) || !answer.EndsWith(Postfix))
            {
                answer = await conversation.Say(string.Format(PromptRegenerate, Prefix, Postfix));
            }
            var codeOnly = string.Join('\n', start) + "\n" + answer[Prefix.Length..^Postfix.Length] + "\n" + string.Join('\n', end);

            await new FileIOPlugin().WriteAsync(targetFilePath, codeOnly);

            regenerate = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Do you like updated code?[/]")
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
}

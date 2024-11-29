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
        var targetFileName = ExternalProcessPlugin.WindowTargetFileName();

        var allFiles = new FileFinderPlugin(_config.GetStringValue("$.working_dir"), Logger).FindCsFiles();
        if (!allFiles.TryGetValue(Path.GetFileNameWithoutExtension(targetFileName), out var targetFilePath))
        {
            AnsiConsole.MarkupLine("[red]Not able to find {0} in workding directory.[/]", targetFileName);
            return 1;
        }
        var fileContent = await new FileIO().ReadAsync(targetFilePath);

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

        var additionalFromInstruction = await ExternalTypesFromInstructionContext(PromptTypesFromInstructions, allFiles, fileContent, Logger);

        var additional = await ExternalContext(allFiles, fileContent);

        List<string> additionalFileContents = [.. additionalFromInstruction, .. additional];
        var filtered = additionalFileContents.Distinct();

        var userMessage = await new PromptBuilder(Logger).CreatePrompt(PromptMain,
            new Dictionary<string, object?> { ["csharp_code"] = fileContent, ["csharp_additional_code"] = string.Join("\n\n", filtered) });

        var completionService = new CompletionService(_config).CreateChatCompletionService();

        var conversation = new Conversation(_config.GetStringValue("$.system"), completionService, Logger);

        var answer = await conversation.Say(userMessage);
        const string Prefix = "```csharp";
        const string Postfix = "```";

        async Task action(string answer)
        {
            var codeOnly = string.Join('\n', start) + "\n" + answer[Prefix.Length..^Postfix.Length] + "\n" + string.Join('\n', end);
            await new FileIO().WriteAsync(targetFilePath, codeOnly);
            new DotNetPlugin(_config.GetStringValue("$.working_dir"), Logger).FormatFile(targetFilePath);
        }

        await FixGeneratedOutput(conversation, answer, action, PromptRegenerate, Prefix, Postfix);

        return 0;
    }
}

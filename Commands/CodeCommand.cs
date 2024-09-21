
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

internal sealed class CodeCommand : BaseCommand<CodeCommand.Settings>
{
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
        var fileContent = await new FileIOPlugin().ReadAsync(targetFilePath);

        var additionalFromInstruction = await ExternalTypesFromInstructionContext(_config.GetStringValue("$.code.types_from_code"), allFiles, fileContent, Logger);

        var additional = await ExternalContext(allFiles, fileContent);

        List<string> additionalFileContents = [.. additionalFromInstruction, .. additional];
        var filtered = additionalFileContents.Distinct();

        var additionalContext = filtered.Any() ?
            await new PromptFactory(Logger).RenderPrompt(_config.GetStringValue("$.code.additional_context"),
                new Dictionary<string, object?> { ["code"] = string.Join("\n\n", filtered) }) : string.Empty;

        if (!string.IsNullOrWhiteSpace(additionalContext))
        {
            additionalContext += "\n";
        }

        var userMessage = await new PromptFactory(Logger).RenderPrompt(_config.GetStringValue("$.code.user_message_code"),
            new Dictionary<string, object?> { ["code"] = fileContent });

        var completionService = new CompletionService(_config).CreateChatCompletionService();

        var conversation = new Conversation(_config.GetStringValue("$.code.system"), completionService, Logger);

        var answer = await conversation.Say(additionalContext + userMessage);

        const string Prefix = "```csharp";
        const string Postfix = "```";

        async Task action(string code)
        {
            await new FileIOPlugin().WriteAsync(targetFilePath, code);
        }

        await FixGeneratedOutput(conversation, answer, action, _config.GetStringValue("$.code.regenerate"), Prefix, Postfix);

        return 0;
    }
}

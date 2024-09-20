
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Core;
using Spectre.Console;
using Spectre.Console.Cli;

internal sealed class ExplainCommand : BaseCommand<ExplainCommand.Settings>
{
    private string PromptMain => _config.GetStringValue("$.explain.main");
    private string PromptRegenerate => _config.GetStringValue("$.explain.regenerate");

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

        var additional = await ExternalContext(allFiles, fileContent);

        var filtered = additional.Distinct();

        var userMessage = await new PromptFactory(Logger).RenderPrompt(PromptMain,
            new Dictionary<string, object?> { ["csharp_code"] = fileContent, ["csharp_additional_code"] = string.Join("\n\n", filtered) });

        var completionService = new CompletionService(_config).CreateChatCompletionService();

        var conversation = new Conversation(_config.GetStringValue("$.system"), completionService, Logger);

        var answer = await conversation.Say(userMessage);
        const string Prefix = "```csharp";
        const string Postfix = "```";

        async Task action(string code)
        {
            await new FileIOPlugin().WriteAsync(targetFilePath, code);
        }

        await FixGeneratedOutput(conversation, answer, action, PromptRegenerate, Prefix, Postfix);

        return 0;
    }
}


using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Core;
using Spectre.Console;
using Spectre.Console.Cli;

internal sealed class JsonCommand : BaseCommand<JsonCommand.Settings>
{
    private string PromptMain => _config.GetStringValue("$.json.main");
    private string PromptRegenerate => _config.GetStringValue("$.json.regenerate");

    public sealed class Settings : CommandSettings
    {

    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var targetFileName = new ExternalProcessPlugin().VSCodeTargetFileName();

        var allFiles = new FileFinderPlugin(_config.GetStringValue("$.working_dir"), Logger).FindJsonFiles();
        if (!allFiles.TryGetValue(Path.GetFileNameWithoutExtension(targetFileName), out var targetFilePath))
        {
            AnsiConsole.MarkupLine("[red]Not able to find {0} in workding directory.[/]", targetFileName);
            return 1;
        }
        var fileContent = await new FileIOPlugin().ReadAsync(targetFilePath);

        var userMessage = await new PromptFactory(Logger).RenderPrompt(PromptMain,
            new Dictionary<string, object?> { ["json_data"] = fileContent });

        var completionService = new CompletionService(_config).CreateChatCompletionService();

        var conversation = new Conversation(_config, completionService, Logger);

        var answer = await conversation.Say(userMessage);
        const string Prefix = "```json";
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
                    .Title("Do you like [green]created json[/]?")
                    .AddChoices(["Yes", "No"])) == "No";

            if (!regenerate) break;

            var userComment = AnsiConsole.Prompt(new TextPrompt<string>("[red]What is the issue with generated code?[/]"));

            answer = await conversation.Say(userComment);
        }

        return 0;
    }
}

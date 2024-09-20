
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

internal sealed class CoFCommand : BaseCommand<CoFCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {

    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var completionService = new CompletionService(_config).CreateChatCompletionService();

        var conversation = new Conversation(_config.GetStringValue("$.chain-of-thought.system"), completionService, Logger);

        var userMessage = AnsiConsole.Prompt(new TextPrompt<string>("[green]Input:[/]"));

        var answer = await conversation.Say(userMessage);

        var regenerate = true;
        while (regenerate)
        {
            AnsiConsole.Write(new Rule("Output"));
            AnsiConsole.WriteLine(answer);
            answer = AnsiConsole.Prompt(new TextPrompt<string>("[green]Input:[/]").AllowEmpty());

            regenerate = !string.IsNullOrWhiteSpace(answer);

            if (!regenerate) break;

            answer = await conversation.Say(answer);
        }

        return 0;
    }
}

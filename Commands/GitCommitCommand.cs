
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

internal sealed class GitCommitCommand : AsyncCommand<GitCommitCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var gitPlugin = new GitPlugin();
        var output = gitPlugin.GitDiff();
        if (string.IsNullOrWhiteSpace(output))
        {
            AnsiConsole.MarkupLine("[red]Git diff output is empty. Consider stage some files to generate commit message for them.[/]");
            return 1;
        }
        var userMessage = await PromptFactory.RenderPrompt(Env.UserPrompts.GitCommit.Main, new Dictionary<string, object?> { ["diff_output"] = output });

        var conversation = Conversation.StartTalkWith(Persona.SeniorSoftwareDeveloper);

        var answer = await conversation.Say(userMessage);

        var regenerate = true;
        while (regenerate)
        {
            AnsiConsole.Write(new Panel(answer)
            {
                Header = new PanelHeader("Commit message")
            });

            regenerate = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Do you like created [green]commit message[/]?")
                    .AddChoices(["Yes", "No"])) == "No";

            if (!regenerate) break;

            answer = await conversation.Say(Env.UserPrompts.GitCommit.Regenerate);
        }

        gitPlugin.GitCommit(answer);

        return 0;
    }
}

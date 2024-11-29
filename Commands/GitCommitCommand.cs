
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

internal sealed class GitCommitCommand : BaseCommand<GitCommitCommand.Settings>
{
    private string PromptMain => _config.GetStringValue("$.commit.main");

    public sealed class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var gitPlugin = new GitPlugin(_config.GetStringValue("$.working_dir"), _config.GetStringValue("$.remote_repository_name"), Logger);
        var output = gitPlugin.GitDiffStaged();
        if (string.IsNullOrWhiteSpace(output))
        {
            AnsiConsole.MarkupLine("[red]Git diff output is empty. Consider stage some files to generate commit message for them.[/]");
            return 1;
        }
        var userMessage = await new PromptBuilder(Logger).CreatePrompt(PromptMain, new Dictionary<string, object?> { ["diff_output"] = output });

        var completionService = new CompletionService(_config).CreateChatCompletionService();

        var conversation = new Conversation(_config.GetStringValue("$.system"), completionService, Logger);

        var answer = await conversation.Say(userMessage);

        var regenerate = true;
        while (regenerate)
        {
            AnsiConsole.Write(new Panel(answer)
            {
                Header = new PanelHeader("Output")
            });

            regenerate = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Do you like output?[/]")
                    .AddChoices(["Yes", "No"])) == "No";

            if (!regenerate) break;

            var userComment = AnsiConsole.Prompt(new TextPrompt<string>("[red]What is the issue with output?[/]"));

            answer = await conversation.Say(userComment);
        }

        gitPlugin.GitCommit(answer);

        return 0;
    }
}

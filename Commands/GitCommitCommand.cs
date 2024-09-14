
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Core;
using Spectre.Console;
using Spectre.Console.Cli;

internal sealed class GitCommitCommand : BaseCommand<GitCommitCommand.Settings>
{
    private string PromptMain => _config.GetStringValue("$.commit.main");
    private string PromptRegenerate => _config.GetStringValue("$.commit.regenerate");

    public sealed class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var gitPlugin = new GitPlugin(_config.GetStringValue("$.working_dir"), Logger);
        var output = gitPlugin.GitDiff();
        if (string.IsNullOrWhiteSpace(output))
        {
            AnsiConsole.MarkupLine("[red]Git diff output is empty. Consider stage some files to generate commit message for them.[/]");
            return 1;
        }
        var userMessage = await new PromptFactory(Logger).RenderPrompt(PromptMain, new Dictionary<string, object?> { ["diff_output"] = output });

        var completionService = new CompletionService(_config).CreateChatCompletionService();

        var conversation = new Conversation(_config, completionService, Logger);

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
                    .Title("Do you like [green]output[/]?")
                    .AddChoices(["Yes", "No"])) == "No";

            if (!regenerate) break;

            answer = await conversation.Say(PromptRegenerate);
        }

        gitPlugin.GitCommit(answer);

        return 0;
    }
}

using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;
using TextCopy;

internal sealed class GitDiffCommand : BaseCommand<GitDiffCommand.Settings>
{
    private string PromptMain => _config.GetStringValue("$.diff.main");

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[targetBranch]")]
        public string? TargetBranch { get; set; }
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var gitPlugin = new GitPlugin(_config.GetStringValue("$.working_dir"), Logger);
        var currentBranch = gitPlugin.GetCurrentBranch().Trim();

        if (string.IsNullOrWhiteSpace(currentBranch))
        {
            AnsiConsole.MarkupLine("[yellow]Warning: Current branch is empty. Please check your git repository.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine("[green]Current branch:[/] [navy]{0}[/]", currentBranch);

        if (!gitPlugin.GitBranchExists(currentBranch))
        {
            AnsiConsole.MarkupLine("[red]Error: Current branch does not exist.[/]");
            return 3;
        }

        if (string.IsNullOrWhiteSpace(settings.TargetBranch))
        {
            AnsiConsole.MarkupLine("[red]Warning: Target branch is not specified.[/]");
            return 2;
        }

        if (!gitPlugin.GitBranchExists(settings.TargetBranch))
        {
            AnsiConsole.MarkupLine("[red]Error: Target branch does not exist.[/]");
            return 4;
        }

        AnsiConsole.MarkupLine("[green]Target branch:[/] [navy]{0}[/]", settings.TargetBranch);

        var diff = gitPlugin.GitDiffMergeBase(currentBranch, settings.TargetBranch);
        var userMessage = await new PromptFactory(Logger).RenderPrompt(PromptMain, new Dictionary<string, object?> { ["diff_output"] = diff });
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
            var userComment = AnsiConsole.Prompt(new TextPrompt<string>("[green]Anything to fix?[/]").AllowEmpty());
            if (string.IsNullOrWhiteSpace(userComment)) break;
            answer = await conversation.Say(userComment);
        }
        await ClipboardService.SetTextAsync(answer);
        return 0;
    }
}

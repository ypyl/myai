
using System.ComponentModel;
using Microsoft.SemanticKernel;
using Spectre.Console;

[Description("Plugin to work to run git commands.")]
internal sealed class GitPlugin
{
    private readonly string _workingDir;

    public GitPlugin()
    {
        _workingDir = Env.WorkingDir;
    }

    [KernelFunction("git_diff")]
    [Description("Gets a diff of staged files.")]
    [return: Description("Git diff command output")]
    public string GitDiff(bool debug = false)
    {
        return AnsiConsole.Status().Start("Getting git diff...", ctx =>
        {
            return ExternalApp.Execute("git", "diff --staged", (l) =>
            {
                if (!debug) return;
                if (l?.StartsWith("@@") == true)
                    AnsiConsole.MarkupLine("[navy]{0}[/]", l.EscapeMarkup());
                else if (l?.StartsWith("-") == true)
                    AnsiConsole.MarkupLine("[red]{0}[/]", l.EscapeMarkup());
                else if (l?.StartsWith("+") == true)
                    AnsiConsole.MarkupLine("[green]{0}[/]", l.EscapeMarkup());
                else
                    AnsiConsole.MarkupLine("{0}", l.EscapeMarkup());
            }, _workingDir, (e) => { AnsiConsole.MarkupLine("{0}", e.EscapeMarkup()); });
        });
    }

    [KernelFunction("git_commit")]
    [Description("Commit changes in git.")]
    public string GitCommit([Description("Commit message")] string message, bool debug = false)
    {
        return AnsiConsole.Status().Start("Committing changes...", ctx =>
        {
            return ExternalApp.Execute("git", $"commit -m \"{message}\"", (l) =>
            {
                if (!debug) return;
                AnsiConsole.MarkupLine("{0}", l.EscapeMarkup());
            }, _workingDir, (e) => { AnsiConsole.MarkupLine("{0}", e.EscapeMarkup()); });
        });
    }
}

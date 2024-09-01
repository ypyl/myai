
using System.ComponentModel;
using Microsoft.SemanticKernel;
using Spectre.Console;

[Description("Plugin to work to run git commands.")]
internal sealed class GitPlugin
{
    [KernelFunction("git_diff")]
    [Description("Gets a diff of staged files.")]
    [return: Description("Git diff command output")]
    public string GitDiff()
    {
        return AnsiConsole.Status().Start("Getting git diff...", ctx =>
        {
            return new ExternalAppPlugin().ExecuteCommand("git", "diff --staged");
        });
    }

    [KernelFunction("git_commit")]
    [Description("Commit changes in git.")]
    public string GitCommit([Description("Commit message")] string message)
    {
        return AnsiConsole.Status().Start("Committing changes...", ctx =>
        {
            return new ExternalAppPlugin().ExecuteCommand("git",  $"commit -m \"{message}\"");
        });
    }
}

using System.ComponentModel;
using Microsoft.SemanticKernel;
using Serilog;
using Spectre.Console;
[Description("Plugin to work to run git commands.")]
internal sealed class GitPlugin(string workingDir, ILogger logger)
{
    [KernelFunction("git_diff_staged")]
    [Description("Gets a diff of staged files.")]
    [return: Description("Git diff command output")]
    public string GitDiffStaged()
    {
        return AnsiConsole.Status().Start("Getting git diff...", ctx =>
        {
            return new ExternalAppPlugin(workingDir, logger).ExecuteCommand("git", "diff --staged");
        });
    }
    [KernelFunction("git_diff_merge_base")]
    [Description("Gets a diff of the merge base of target and source branches.")]
    [return: Description("Git diff command output")]
    public string GitDiffMergeBase(string sourceBranch, string targetBranch)
    {
        return AnsiConsole.Status().Start("Getting git diff...", ctx =>
        {
            return new ExternalAppPlugin(workingDir, logger).ExecuteCommand("git", $"diff --merge-base {targetBranch} {sourceBranch}");
        });
    }
    [KernelFunction("git_current_branch")]
    [Description("Gets the current branch in git.")]
    [return: Description("The current branch name")]
    public string GetCurrentBranch()
    {
        return new ExternalAppPlugin(workingDir, logger).ExecuteCommand("git", "rev-parse --abbrev-ref HEAD");
    }
    [KernelFunction("git_commit")]
    [Description("Commit changes in git.")]
    public string GitCommit([Description("Commit message")] string message)
    {
        return AnsiConsole.Status().Start("Committing changes...", ctx =>
        {
            return new ExternalAppPlugin(workingDir, logger).ExecuteCommand("git", $"commit -m \"{message}\"");
        });
    }

    [KernelFunction("git_branch_exists")]
    [Description("Checks if a git branch exists.")]
    [return: Description("True if the branch exists, false otherwise")]
    public bool GitBranchExists(string branchName)
    {
        return !string.IsNullOrEmpty(new ExternalAppPlugin(workingDir, logger).ExecuteCommand("git", $"branch --list {branchName}"));
    }
}

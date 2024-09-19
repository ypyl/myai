using System.ComponentModel;
using Microsoft.SemanticKernel;
using Serilog;
using Spectre.Console;

[Description("Plugin to work to run git commands.")]
internal sealed class GitPlugin(string workingDir, string remoteRepositoryName, ILogger logger)
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

    [KernelFunction("git_fetch_branch")]
    [Description("Fetches a branch from remote by name if it exists in any added remote.")]
    public string GitFetchBranch([Description("The name of the branch to fetch")] string branchName)
    {
        var plugin = new ExternalAppPlugin(workingDir, logger);

        // Step 1: Check if the branch exists in the remote repository.
        var checkBranchCommand = $"ls-remote --exit-code {remoteRepositoryName} {branchName}";
        var checkBranchResult = plugin.ExecuteCommand("git", checkBranchCommand);

        if (string.IsNullOrEmpty(checkBranchResult))
        {
            // The branch does not exist in the remote.
            logger.Information($"Branch '{branchName}' does not exist in the remote '{remoteRepositoryName}'.");
            return $"Branch '{branchName}' does not exist in the remote.";
        }

        // Step 2: If the branch exists, fetch it from the remote.
        var fetchCommand = $"fetch {remoteRepositoryName} {branchName}";
        var fetchResult = plugin.ExecuteCommand("git", fetchCommand);

        // Step 3: Return the result of the fetch operation.
        logger.Information($"Successfully fetched branch '{branchName}' from remote.");
        return fetchResult;
    }
}


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

        var finalAnswer = await UITools.ConfirmAnswer(conversation, answer, Env.UserPrompts.GitCommit.Regenerate);

        gitPlugin.GitCommit(finalAnswer);

        return 0;
    }
}

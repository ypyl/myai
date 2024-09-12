
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Core;
using Spectre.Console;
using Spectre.Console.Cli;

internal sealed class GitCommitCommand : AsyncCommand<GitCommitCommand.Settings>
{
    private string PromptMain => _config.GetStringValue("$.commit.main");
    private string PromptRegenerate => _config.GetStringValue("$.commit.regenerate");
    private readonly Config _config = new();

    public sealed class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var logger = CreateLogger();
        var gitPlugin = new GitPlugin(_config.GetStringValue("$.working_dir"), logger);
        var output = gitPlugin.GitDiff();
        if (string.IsNullOrWhiteSpace(output))
        {
            AnsiConsole.MarkupLine("[red]Git diff output is empty. Consider stage some files to generate commit message for them.[/]");
            return 1;
        }
        var userMessage = await new PromptFactory(logger).RenderPrompt(PromptMain, new Dictionary<string, object?> { ["diff_output"] = output });

        var completionService = new CompletionService(_config).CreateChatCompletionService();

        var conversation = new Conversation(_config, completionService, logger);

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

    private ILogger CreateLogger()
    {
        var debug = _config.GetBoolValue("$.debug");
        var logDir = _config.GetStringValue("$.log_dir");
        if (debug && !string.IsNullOrEmpty(logDir) && Directory.Exists(logDir))
        {
            // Generate a unique filename using a timestamp
            var logFileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var logFilePath = Path.Combine(logDir, logFileName);

            // Initialize the logger with the constructed file path
            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(logFilePath, retainedFileCountLimit: 5)
                .CreateLogger();
        }
        return Logger.None;
    }
}

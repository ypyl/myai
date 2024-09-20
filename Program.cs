using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<GitCommitCommand>("commit")
        .WithDescription("Commit staged changes using generated message")
        .WithExample("commit");
    config.AddCommand<GitDiffCommand>("diff")
        .WithDescription("SUm up diff between current branch and target")
        .WithExample("diff [targetBranch]");
    config.AddCommand<CodeCommand>("code")
        .WithDescription("Complete code in opened VSCode file")
        .WithExample("code");
    config.AddCommand<SnippetCommand>("snippet")
        .WithDescription("Implement code snippet in opened VSCode file")
        .WithExample("snippet");
    config.AddCommand<JsonCommand>("json")
        .WithDescription("Complete json in opened VSCode file")
        .WithExample("json");
    config.AddCommand<ExplainCommand>("explain")
        .WithDescription("Explain code by adding comments to it in opened VSCode file")
        .WithExample("explain");
    config.AddCommand<ExplainCommand>("logging")
        .WithDescription("Add logging to code by using ILogger to it in opened VSCode file")
        .WithExample("logging");
    config.AddCommand<CoFCommand>("cof")
        .WithDescription("Chain of thought")
        .WithExample("cof");

    config.SetExceptionHandler((ex, typeResolver) =>
    {
        AnsiConsole.WriteException(ex, ExceptionFormats.Default);
    });
});
return app.Run(args);

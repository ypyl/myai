using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<GitCommitCommand>("commit")
        .WithDescription("Commit staged changes using generated message")
        .WithExample("commit");
    config.AddCommand<CodeCommand>("code")
        .WithDescription("Complete code in opened VSCode file")
        .WithExample("code");
    config.AddCommand<JsonCommand>("json")
        .WithDescription("Complete json in opened VSCode file")
        .WithExample("json");
    config.AddCommand<ExplainCommand>("explain")
        .WithDescription("Explain code by adding comments to it in opened VSCode file")
        .WithExample("explain");
    config.AddCommand<ExplainCommand>("logging")
        .WithDescription("Add logging to code by using ILogger to it in opened VSCode file")
        .WithExample("logging");

    config.SetExceptionHandler((ex, typeResolver) =>
    {
        AnsiConsole.WriteException(ex, ExceptionFormats.Default);
    });
});
return app.Run(args);

using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<GitCommitCommand>("commit")
        .WithDescription("Commit staged changes using generated message")
        .WithExample("commit")
        .WithExample("commit", "--debug");
    config.AddCommand<CodeCommand>("code")
        .WithDescription("Complete code in opened VSCode file")
        .WithExample("code")
        .WithExample("code", "--debug");
});
return app.Run(args);

using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<GitCommitCommand>("commit")
        .WithDescription("Commit staged changes using generated message")
        .WithExample("commit")
        .WithExample("commit", "--debug");
});
return app.Run(args);

// var processes = Process.GetProcessesByName("Code");

// if(processes.Length == 0)
// {
//     Console.WriteLine(processes.Length);
// }
// else
// {
//     foreach(var process in processes)
//     {
//         Console.WriteLine(process.MainWindowTitle);
//     }
// }

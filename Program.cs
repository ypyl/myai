using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;
using System.Text;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using dotenv.net;

DotEnv.Load();

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<GitCommitCommand>("commit")
        .WithDescription("Commit staged changes using generated message")
        .WithExample("commit")
        .WithExample("commit", "--debug");
});
return app.Run(args);

internal class CustomDelegatingHandler() : DelegatingHandler(new HttpClientHandler())
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var envVars = DotEnv.Read();
        request.RequestUri = new Uri(request.RequestUri.ToString().Replace("https://api.openai.com/v1", envVars["GROQ_ENDPOINT"]));
        return await base.SendAsync(request, cancellationToken);
    }
}

internal sealed class ExternalApp
{
    public static string Execute(string fileName, string arguments, Action<string?> processFunc, string workingDirectory, Action<string?> processErrorFunc)
    {
        using Process process = new();

        var outputBuilder = new StringBuilder();

        var processStartInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            Arguments = arguments,
            FileName = fileName,
            WorkingDirectory = workingDirectory,
        };

        process.StartInfo = processStartInfo;

        process.OutputDataReceived += new DataReceivedEventHandler
        (
            delegate (object sender, DataReceivedEventArgs e)
            {
                processFunc(e.Data);
                outputBuilder.AppendLine(e.Data);
            }
        );
        process.ErrorDataReceived += new DataReceivedEventHandler
        (
            delegate (object sender, DataReceivedEventArgs e)
            {
                processErrorFunc(e.Data);
            }
        );
        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
        process.CancelOutputRead();

        return outputBuilder.ToString();
    }
}

[Description("Plugin to work to run git commands.")]
internal sealed class GitPlugin
{
    private readonly string _workingDir;

    public GitPlugin()
    {
        _workingDir = DotEnv.Read()["WORKING_DIR"];
    }

    [KernelFunction("git_diff")]
    [Description("Gets a diff of staged files.")]
    [return: Description("Git diff command output")]
    public string GitDiff(bool debug = false)
    {
        AnsiConsole.Write(new Rule(nameof(GitDiff)));
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
    }

    [KernelFunction("git_commit")]
    [Description("Commit changes in git.")]
    public string GitCommit([Description("Commit message")] string message, bool debug = false)
    {
        AnsiConsole.Write(new Rule(nameof(GitCommit)));
        return ExternalApp.Execute("git", $"commit -m \"{message}\"", (l) =>
        {
            if (!debug) return;
            AnsiConsole.MarkupLine("{0}", l.EscapeMarkup());
        }, _workingDir, (e) => { AnsiConsole.MarkupLine("{0}", e.EscapeMarkup()); });
    }
}

internal enum Persona
{
    SeniorSoftwareDeveloper,
}

internal sealed class Conversation
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly ChatHistory _chatHistory;
    private readonly bool _debug;

    private Conversation(ChatHistory chatHisotry, bool debug = false)
    {
        var envVars = DotEnv.Read();
        _chatCompletionService = envVars["AI_PROVIDER"] switch
        {
            "GROQ" => new OpenAIChatCompletionService(
                modelId: envVars["GROQ_MODEL_ID"],
                httpClient: new(new CustomDelegatingHandler()),
                apiKey: envVars["GROQ_API_KEY"]),
            "AZURE_OPENAI" => new AzureOpenAIChatCompletionService(
                deploymentName: envVars["AZURE_OPENAI_DEPLOYMENT"],
                endpoint: envVars["AZURE_OPENAI_ENDPOINT"],
                apiKey: envVars["AZURE_OPENAI_API_KEY"]),
        };
        _chatHistory = chatHisotry;
        _debug = debug;
        MessageOutputAsync(_chatHistory);
    }

    public static Conversation StartTalkWith(Persona persona)
    {
        AnsiConsole.Write(new Rule(nameof(StartTalkWith) + " " + Enum.GetName(typeof(Persona), persona)));
        var systemPrompt = persona switch
        {
            Persona x when x == Persona.SeniorSoftwareDeveloper => "You are senior software developer.",
            _ => string.Empty,
        };
        return new Conversation(new ChatHistory(systemPrompt));
    }

    public async Task<string> Say(string message)
    {
        _chatHistory.AddUserMessage(message);
        MessageOutputAsync(_chatHistory);
        var reply = await _chatCompletionService.GetChatMessageContentAsync(_chatHistory);
        _chatHistory.Add(reply);
        MessageOutputAsync(_chatHistory);
        return reply.ToString();
    }

    private void MessageOutputAsync(ChatHistory chatHistory)
    {
        if (!_debug) return;
        var message = chatHistory.Last();
        var color = message.Role switch
        {
            AuthorRole x when x == AuthorRole.System => "red",
            AuthorRole x when x == AuthorRole.User => "green",
            AuthorRole x when x == AuthorRole.Assistant => "navy",
            _ => null,
        };
        if (color is null)
            AnsiConsole.MarkupLine($"{message.Role}: {message.Content.EscapeMarkup()}");
        else
            AnsiConsole.MarkupLine($"[{color}]{message.Role}[/]: {message.Content.EscapeMarkup()}");
    }
}

internal sealed class PromptFactory
{
    private static readonly KernelPromptTemplateFactory _promptTemplateFactory = new KernelPromptTemplateFactory();

    public static async Task<string> RenderPrompt(string template, IDictionary<string, object?> parameters, bool debug = false)
    {
        AnsiConsole.Write(new Rule(nameof(RenderPrompt)));
        var arguments = new KernelArguments(parameters);
        var prompt = await _promptTemplateFactory.Create(new PromptTemplateConfig(template)).RenderAsync(new Kernel(), arguments);
        if (debug) AnsiConsole.MarkupLine($"[green]template[/]: {prompt.EscapeMarkup()}");
        return prompt;
    }
}

internal sealed class GitCommitCommand : AsyncCommand<GitCommitCommand.Settings>
{
    private const string commitMessageExample = """
Add feature availability check to semantic search provider

Integrated feature availability check to enable/disable semantic chunking in search queries.
""";
    private const string template = """
Your task is to create a commit message. It must contain title (50 characters) and body (100 characters).
There is an example of output:
{{ $commit_message_sample }}
You output will be used in 'git commit -m \"OUTPUT_HERE\"'.
Return only commit message.
Please create commit message for the following diff:

{{ $diff_output }}
""";
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-d|--debug")]
        [DefaultValue(false)]
        public bool Debug { get; set; }
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var gitPlugin = new GitPlugin();
        var output = gitPlugin.GitDiff(settings.Debug);
        var userMessage = await PromptFactory.RenderPrompt(template, new Dictionary<string, object?> { ["commit_message_sample"] = commitMessageExample, ["diff_output"] = output }, settings.Debug);

        var conversation = Conversation.StartTalkWith(Persona.SeniorSoftwareDeveloper);

        var answer = await conversation.Say(userMessage);

        var regenerate = true;
        while (regenerate)
        {
            AnsiConsole.Write(new Panel(answer)
            {
                Header = new PanelHeader("Commit message")
            });

            regenerate = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Do you like created [green]commit message[/]?")
                    .AddChoices(["Yes", "No"])) == "No";

            answer = await conversation.Say("I don't like created commite message. Please create a new one.");
        }

        gitPlugin.GitCommit(answer, settings.Debug);

        return 0;
    }
}

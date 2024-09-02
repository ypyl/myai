

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Spectre.Console;

internal enum Persona
{
    SeniorSoftwareDeveloper,
}

internal class CustomDelegatingHandler() : DelegatingHandler(new HttpClientHandler())
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString().Replace("https://api.openai.com/v1", Env.GorqEndpoint) ?? throw new ArgumentNullException(nameof(request));
        request.RequestUri = new Uri(url);
        return await base.SendAsync(request, cancellationToken);
    }
}

internal sealed class Conversation
{
    private readonly Kernel _kernel;
    private readonly ChatHistory _chatHistory;
    private readonly bool _debug;

    private Conversation(Persona persona)
    {
        var builder = Kernel.CreateBuilder();
        var systemPrompt = persona switch
        {
            Persona x when x == Persona.SeniorSoftwareDeveloper => Env.SystemPrompts.SeniorSoftwareDeveloper,
            _ => string.Empty,
        };
        if (Env.AIProvider == "GROQ")
        {
            builder.AddOpenAIChatCompletion(modelId: Env.GroqModelId,
                httpClient: new(new CustomDelegatingHandler()),
                apiKey: Env.GroqApiKey);
        }
        if (Env.AIProvider == "AZURE_OPENAI")
        {
            builder.AddAzureOpenAIChatCompletion(deploymentName: Env.AzureOpenAIDeployment,
                endpoint: Env.AzureOpenAIEndpoint,
                apiKey: Env.AzureOpenAIApiKey);
        }
        _kernel = builder.Build();
        _chatHistory = new ChatHistory(systemPrompt);
        _debug = Env.Debug;
        MessageOutputAsync(_chatHistory);
    }

    public static Conversation StartTalkWith(Persona persona)
    {
        return AnsiConsole.Status().Start($"Starting conversation with {Enum.GetName(typeof(Persona), persona)}...", ctx =>
        {
            return new Conversation(persona);
        });
    }

    public async Task<string> Say(string message)
    {
        _chatHistory.AddUserMessage(message);
        MessageOutputAsync(_chatHistory);
        var reply = await AnsiConsole.Status().StartAsync("Waiting model answer...", async ctx =>
        {
            return await _kernel.GetRequiredService<IChatCompletionService>()
                .GetChatMessageContentAsync(_chatHistory, kernel: _kernel);
        });
        _chatHistory.Add(reply);
        MessageOutputAsync(_chatHistory);
        return reply.ToString();
    }

    public async Task<string> QA(string question)
    {
        return await AnsiConsole.Status().StartAsync("Waiting model answer...", async ctx =>
        {
            var result = await _kernel.InvokePromptAsync(question);
            return result.ToString();
        });
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

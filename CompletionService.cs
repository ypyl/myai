using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Spectre.Console;

internal class CustomDelegatingHandler(string endpoint) : DelegatingHandler(new HttpClientHandler())
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString().Replace("https://api.openai.com/v1", endpoint) ?? throw new ArgumentNullException(nameof(request));
        request.RequestUri = new Uri(url);
        return await base.SendAsync(request, cancellationToken);
    }
}

internal sealed class CompletionService(Config settings)
{
    public IChatCompletionService CreateChatCompletionService()
    {
        var endpoint = settings.GetStringValue("$.model.endpoint");
        if (endpoint.Contains("groq"))
        {
            var modelId = settings.GetStringValue("$.model.model_id");
            AnsiConsole.MarkupLine($"[blue]Model id: {modelId}[/]");
            return new OpenAIChatCompletionService(settings.GetStringValue("$.model.model_id"), settings.GetStringValue("$.model.api_key"), httpClient: new(new CustomDelegatingHandler(endpoint)));
        }
        else
        {
            var deployment = settings.GetStringValue("$.model.deployment");
            AnsiConsole.MarkupLine($"[blue]Model/Deployment name: {deployment}[/]");
            return new AzureOpenAIChatCompletionService(deployment, endpoint, apiKey: settings.GetStringValue("$.model.api_key"));
        }
    }
}

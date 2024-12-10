namespace MyAi.Code;

public sealed class ModelOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "llama-3.3-70b-versatile";
    public string Endpoint { get; set; } = "https://api.groq.com/openai/v1";
}

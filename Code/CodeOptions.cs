namespace MyAi.Code;

public sealed class CodeOptions
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string InputPrompt { get; set; } = string.Empty;
    public string AdditionalPrompt { get; set; } = string.Empty;
    public string TypesFromInstructionsPrompt { get; set; } = string.Empty;
    public string RegeneratePrompt { get; set; } = string.Empty;
}

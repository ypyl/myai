namespace MyAi.Code;

public sealed class CodeOptions
{
    public List<string> CommentBasedCodePrompts { get; set; } = [];
    public List<string> InstructionBasedCodePrompts { get; set; } = [];
    public string RegeneratePrompt { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string Postfix { get; set; } = string.Empty;
}

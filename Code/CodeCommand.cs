
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace MyAi.Code;

public sealed class CodeCommand : AsyncCommand<CodeCommand.Settings>
{
    private readonly CommentBasedCodeAgent _commentBasedCodeAgent;
    private readonly CodeAgent _codeAgent;

    public CodeCommand(CommentBasedCodeAgent commentBasedCodeAgent, CodeAgent codeAgent)
    {
        _commentBasedCodeAgent = commentBasedCodeAgent;
        _codeAgent = codeAgent;
    }
    public sealed class Settings : CommandSettings
    {
        [Description("Instruction to the code generator.")]
        [CommandArgument(0, "[instruction]")]
        public string? Instruction { get; init; }
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        if (settings.Instruction is null)
        {
            return await _commentBasedCodeAgent.Run() ? 0 : 1;
        }
        return await _codeAgent.Run(settings.Instruction) ? 0 : 1;
    }
}

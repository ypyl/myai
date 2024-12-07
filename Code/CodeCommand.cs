
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace MyAi.Code;

public sealed class CodeCommand : AsyncCommand<CodeCommand.Settings>
{
    private readonly CodeAgent _codeGenerator;

    public CodeCommand(CodeAgent codeGenerator)
    {
        _codeGenerator = codeGenerator;
    }
    public sealed class Settings : CommandSettings
    {
        [Description("Main instruction to generate code.")]
        [CommandArgument(0, "[instruction]")]
        public string? Instruction { get; init; }
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        return await _codeGenerator.Run(settings.Instruction) ? 0 : 1;
    }
}

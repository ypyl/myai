
using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace MyAi.Code;

public sealed class CodeCommand : AsyncCommand<CodeCommand.Settings>
{
    private readonly GenerateCodeAgent _codeGenerator;

    public CodeCommand(GenerateCodeAgent codeGenerator)
    {
        _codeGenerator = codeGenerator;
    }
    public sealed class Settings : CommandSettings
    {

    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        return await _codeGenerator.Run() ? 0 : 1;
    }
}

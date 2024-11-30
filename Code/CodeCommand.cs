
using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace MyAi.Code;

public sealed class CodeCommand(GenerateCode generateCode) : AsyncCommand<CodeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {

    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        return await generateCode.Run() ? 0 : 1;
    }
}

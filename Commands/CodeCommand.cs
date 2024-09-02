
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

internal sealed class CodeCommand : AsyncCommand<CodeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var targetFileName = new ExternalProcessPlugin().VSCodeTargetFileName();
        AnsiConsole.MarkupLine(targetFileName);

        var allFiles = new CsFileFinderPlugin().FindCsFiles();
        if (!allFiles.TryGetValue(Path.GetFileNameWithoutExtension(targetFileName), out var targetFilePath))
        {
            AnsiConsole.MarkupLine("[red]Not able to find {0} in workding directory.[/]", targetFileName);
            return 1;
        }
        var fileContent = await new FileIOPlugin().ReadAsync(targetFilePath);

        var additionalFromInstruction = await ExternalTypesFromInstructionContext(allFiles, fileContent);

        var additional = await ExternalContext(allFiles, fileContent);

        var userMessage = await PromptFactory.RenderPrompt(Env.UserPrompts.Code.Main,
            new Dictionary<string, object?> { ["csharp_code"] = fileContent, ["csharp_additional_code"] = string.Join("\n\n", [.. additionalFromInstruction, .. additional]) });

        var conversation = Conversation.StartTalkWith(Persona.SeniorSoftwareDeveloper);

        var answer = await conversation.Say(userMessage);
        const string Prefix = "```csharp";
        const string Postfix = "```";

        var regenerate = true;
        while (regenerate)
        {
            while (!answer.StartsWith(Prefix) || !answer.EndsWith(Postfix))
            {
                // TODO loop protection
                answer = await conversation.Say(string.Format(Env.UserPrompts.Code.Regenerate, Prefix, Postfix));
            }
            var codeOnly = answer[Prefix.Length..^Postfix.Length];

            if (Env.Debug)
            {
                AnsiConsole.Write(new Panel(codeOnly.EscapeMarkup())
                {
                    Header = new PanelHeader("Generated code")
                });
            }

            await new FileIOPlugin().WriteAsync(targetFilePath, codeOnly);

            regenerate = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Do you like [green]created code[/]?")
                    .AddChoices(["Yes", "No"])) == "No";

            if (!regenerate) break;

            var userComment = AnsiConsole.Prompt(new TextPrompt<string>("What is the [red]issue[/] with [green]generated code[/]?"));

            answer = await conversation.Say(userComment);
        }

        return 0;
    }

    private async Task<List<string>> ExternalTypesFromInstructionContext(Dictionary<string, string> allFiles, string targetFileContent)
    {
        var conversation = Conversation.StartTalkWith(Persona.SeniorSoftwareDeveloper);

        var userMessage = await PromptFactory.RenderPrompt(Env.UserPrompts.Code.TypesFromInstructions,
            new Dictionary<string, object?> { ["csharp_code"] = targetFileContent });

        var answer = await conversation.Say(userMessage);
        var typesFromInstructions = answer.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        foreach (var path in allFiles.Where(x => typesFromInstructions.Contains(x.Key)).Select(x => x.Value))
        {
            var content = await new FileIOPlugin().ReadAsync(path);
            if (Env.Debug)
            {
                AnsiConsole.MarkupLine("[green]External context[/]: [navy]{0}[/]", path);
            }
            result.Add(content);
        }
        return result;
    }

    private async Task<List<string>> ExternalContext(Dictionary<string, string> allFiles, string targetFile)
    {
        var externalTypes = new CsNonStandardTypeExtractorPlugin().ExtractNonStandardTypes(targetFile);
        var result = new List<string>();
        foreach (var path in allFiles.Where(x => externalTypes.Contains(x.Key)).Select(x => x.Value))
        {
            var content = await new FileIOPlugin().ReadAsync(path);
            if (Env.Debug)
            {
                AnsiConsole.MarkupLine("[green]External context[/]: [navy]{0}[/]", path);
            }
            result.Add(content);
        }
        return result;
    }
}


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

        var userMessage = await PromptFactory.RenderPrompt(Env.UserPrompts.Code, new Dictionary<string, object?> { ["csharp_code"] = fileContent });

        var conversation = Conversation.StartTalkWith(Persona.SeniorSoftwareDeveloper);

        var answer = await conversation.Say(userMessage);
        const string Prefix = "```csharp";
        const string Postfix = "```";

        var regenerate = true;
        while (regenerate)
        {
            while (!answer.StartsWith(Prefix) || !answer.EndsWith(Postfix))
            {
                answer = await conversation.Say($"You must wrap the code by '{Prefix}' and '{Postfix}' as it will be extracted and saved to the file.");
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
}

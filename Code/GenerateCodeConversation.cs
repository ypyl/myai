using Microsoft.Extensions.AI;
using MyAi.Tools;
using Spectre.Console;

namespace MyAi.Code;

public class GenerateCodeConversation(ExternalProcess externalProcess, VSCode VSCode, CodeTools codeTools, WorkingDirectory workingDirectory,
    ExternalTypesFromInstructionsConversation externalTypesFromInstructionContext, Conversation conversation, FileIO fileIO,
    AutoFixLlmAnswer autoFixLlmAnswer, DirectoryPacker directoryPacker)
{
    const string Prefix = "```csharp";
    const string Postfix = "```";

    public async Task<bool> Run()
    {
        var targetWindowTitle = externalProcess.GetFocusedWindowTitle();
        AnsiConsole.MarkupLine("[green]Focused window title:[/] {0}", targetWindowTitle.EscapeMarkup());
        var targetFileName = VSCode.ParseWindowTitle(targetWindowTitle);
        var workingDir = workingDirectory.GetWorkingDirectory();
        AnsiConsole.MarkupLine("[blue]Working directory:[/] {0}", workingDir.EscapeMarkup());
        var codeLangugage = codeTools.GetCodeLanguage(targetFileName);
        var codeOptions = codeTools.GetCodeOptions(codeLangugage);
        var allFiles = codeTools.FindFilesByLanguage(workingDir, codeLangugage);
        if (!allFiles.TryGetValue(Path.GetFileNameWithoutExtension(targetFileName), out var targetFilePath))
        {
            AnsiConsole.MarkupLine("[red]Not able to find {0} in workding directory.[/]", targetFileName);
            return false;
        }

        var fileContent = directoryPacker.PackFiles([targetFilePath]);

        var additionalFromInstruction = await externalTypesFromInstructionContext.Extract(codeOptions.TypesFromInstructionsPrompt, allFiles, fileContent);

        AnsiConsole.MarkupLine("[fuchsia]Extracting external types from the target code.[/]");
        var externalTypes = codeTools.GetExternalTypes(codeLangugage, fileContent);
        AnsiConsole.MarkupLine("[fuchsia]Getting content of external type files.[/]");
        var additionalFromFile = await codeTools.GetContentOfExternalTypes(allFiles, externalTypes);

        List<string> additionalFileContents = [.. additionalFromInstruction, .. additionalFromFile];
        var filtered = additionalFileContents.Distinct();

        var additionalContext = directoryPacker.PackFiles([.. filtered]);

        conversation.AddMessage(ChatRole.System, codeOptions.SystemPrompt);
        conversation.AddMessage(ChatRole.User, codeOptions.InputPrompt, fileContent);
        conversation.AddMessage(ChatRole.User, codeOptions.AdditionalPrompt, additionalContext);

        string? userComment;
        do
        {
            await conversation.CompleteAsync();
            var answer = await autoFixLlmAnswer.RetrieveCodeFragment(conversation, IsCodeOnly, codeOptions.RegeneratePrompt);
            if (answer is null) return false;
            var codeOnly = answer[Prefix.Length..^Postfix.Length];

            await fileIO.WriteAsync(targetFilePath, codeOnly);
            userComment = AnsiConsole.Prompt(new TextPrompt<string>("[green]Anything to fix?[/]").AllowEmpty());
            conversation.AddMessage(ChatRole.User, userComment);
        }
        while (!string.IsNullOrEmpty(userComment));

        return true;
    }

    private static bool IsCodeOnly(string result) => result.StartsWith(Prefix) && result.EndsWith(Postfix);
}

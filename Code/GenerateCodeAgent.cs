using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using MyAi.Tools;
using Spectre.Console;

namespace MyAi.Code;

public class GenerateCodeAgent(ExternalProcess externalProcess, VSCode VSCode, CodeTools codeTools, WorkingDirectory workingDirectory,
    ExternalTypesFromInstructionsAgent externalTypesFromInstructionsAgent, Conversation conversation, FileIO fileIO,
    AutoFixLlmAnswer autoFixLlmAnswer, DirectoryPacker directoryPacker, ILogger<GenerateCodeAgent> logger)
{
    const string Prefix = "```csharp";
    const string Postfix = "```";

    public async Task<bool> Run()
    {
        var targetWindowTitle = externalProcess.GetFocusedWindowTitle();
        logger.LogInformation("Focused window title: {targetWindowTitle}", targetWindowTitle);
        var targetFileName = VSCode.ParseWindowTitle(targetWindowTitle);
        var workingDir = workingDirectory.GetWorkingDirectory();
        logger.LogInformation("Working directory: {workingDir}", workingDir);
        var codeLangugage = codeTools.GetCodeLanguage(targetFileName);
        var codeOptions = codeTools.GetCodeOptions(codeLangugage);
        var allFiles = codeTools.FindFilesByLanguage(workingDir, codeLangugage);
        if (!allFiles.TryGetValue(Path.GetFileNameWithoutExtension(targetFileName), out var targetFilePath))
        {
            logger.LogError("Not able to find {targetFileName} in working directory.", targetFileName);
            return false;
        }

        var fileContent = directoryPacker.PackFiles(new[] { targetFilePath });

        var additionalFromInstruction = await externalTypesFromInstructionsAgent.Run(codeOptions.TypesFromInstructionsPrompt, allFiles, fileContent);

        logger.LogInformation("Extracting external types from the {targetFilePath}.", targetFilePath);
        var externalTypes = codeTools.GetExternalTypes(codeLangugage, fileContent);
        logger.LogInformation("External types: {externalTypes}", string.Join(", ", externalTypes));
        logger.LogInformation("Getting content of external type files.");
        var additionalFromFile = await codeTools.GetContentOfExternalTypes(allFiles, externalTypes);

        List<string> additionalFileContents = [.. additionalFromInstruction.Concat(additionalFromFile)];
        var filtered = additionalFileContents.Distinct();

        var additionalContext = directoryPacker.PackFiles(filtered.ToArray());

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

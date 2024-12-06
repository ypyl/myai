using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using MyAi.Tools;
using Spectre.Console;

namespace MyAi.Code;

public class GenerateCodeAgent
{
    public GenerateCodeAgent(ExternalProcess externalProcess, VSCode VSCode, CodeTools codeTools, WorkingDirectory workingDirectory,
        ExternalTypesFromInstructionsAgent externalTypesFromInstructionsAgent, Conversation conversation, FileIO fileIO,
        AutoFixLlmAnswer autoFixLlmAnswer, DirectoryPacker directoryPacker, ILogger<GenerateCodeAgent> logger)
    {
        _externalProcess = externalProcess;
        _VSCode = VSCode;
        _codeTools = codeTools;
        _workingDirectory = workingDirectory;
        _externalTypesFromInstructionsAgent = externalTypesFromInstructionsAgent;
        _conversation = conversation;
        _fileIO = fileIO;
        _autoFixLlmAnswer = autoFixLlmAnswer;
        _directoryPacker = directoryPacker;
        _logger = logger;
    }

    const string Prefix = "```csharp";
    const string Postfix = "```";
    private readonly ExternalProcess _externalProcess;

    public VSCode _VSCode { get; }

    private readonly CodeTools _codeTools;
    private readonly WorkingDirectory _workingDirectory;
    private readonly ExternalTypesFromInstructionsAgent _externalTypesFromInstructionsAgent;
    private readonly Conversation _conversation;
    private readonly FileIO _fileIO;
    private readonly AutoFixLlmAnswer _autoFixLlmAnswer;
    private readonly DirectoryPacker _directoryPacker;
    private readonly ILogger<GenerateCodeAgent> _logger;

    public async Task<bool> Run()
    {
        var targetWindowTitle = _externalProcess.GetFocusedWindowTitle();

        var targetFileName = _VSCode.ParseWindowTitle(targetWindowTitle);
        var workingDir = _workingDirectory.GetWorkingDirectory();
        var codeLangugage = _codeTools.GetCodeLanguage(targetFileName);
        var codeOptions = _codeTools.GetCodeOptions(codeLangugage);
        var allFiles = _codeTools.FindFilesByLanguage(workingDir, codeLangugage);
        if (!allFiles.TryGetValue(Path.GetFileNameWithoutExtension(targetFileName), out var targetFilePath))
        {
            _logger.LogError("Not able to find {targetFileName} in working directory.", targetFileName);
            return false;
        }

        var fileContent = _directoryPacker.PackFiles(new[] { targetFilePath });

        var additionalFromInstruction = await _externalTypesFromInstructionsAgent.Run(codeOptions.TypesFromInstructionsPrompt, allFiles, fileContent);
        var externalTypes = _codeTools.GetExternalTypes(codeLangugage, fileContent);

        var additionalFromFile = await _codeTools.GetContentOfExternalTypes(allFiles, externalTypes);

        List<string> additionalFileContents = [.. additionalFromInstruction.Concat(additionalFromFile)];
        var filtered = additionalFileContents.Distinct();

        var additionalContext = _directoryPacker.PackFiles(filtered.ToArray());

        _conversation.AddMessage(ChatRole.System, codeOptions.SystemPrompt);
        _conversation.AddMessage(ChatRole.User, codeOptions.InputPrompt, fileContent);
        _conversation.AddMessage(ChatRole.User, codeOptions.AdditionalPrompt, additionalContext);

        string? userComment;
        do
        {
            await _conversation.CompleteAsync();
            var answer = await _autoFixLlmAnswer.RetrieveCodeFragment(_conversation, IsCodeOnly, codeOptions.RegeneratePrompt);
            if (answer is null) return false;
            var codeOnly = answer[Prefix.Length..^Postfix.Length];

            await _fileIO.WriteAsync(targetFilePath, codeOnly);
            userComment = AnsiConsole.Prompt(new TextPrompt<string>("[green]Anything to fix?[/]").AllowEmpty());
            _conversation.AddMessage(ChatRole.User, userComment);
        }
        while (!string.IsNullOrEmpty(userComment));

        return true;
    }

    private static bool IsCodeOnly(string result) => result.StartsWith(Prefix) && result.EndsWith(Postfix);
}

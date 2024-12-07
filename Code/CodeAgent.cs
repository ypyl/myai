using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using MyAi.Tools;

namespace MyAi.Code;

public class CodeAgent
{
    public CodeAgent(ExternalProcess externalProcess, VSCode VSCode, CodeTools codeTools, WorkingDirectory workingDirectory,
        ExternalTypesFromInstructionsAgent externalTypesFromInstructionsAgent, Conversation conversation, FileIO fileIO,
        AutoFixLlmAnswer autoFixLlmAnswer, DirectoryPacker directoryPacker, ILogger<CodeAgent> logger)
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

    private readonly ExternalProcess _externalProcess;

    public VSCode _VSCode { get; }

    private readonly CodeTools _codeTools;
    private readonly WorkingDirectory _workingDirectory;
    private readonly ExternalTypesFromInstructionsAgent _externalTypesFromInstructionsAgent;
    private readonly Conversation _conversation;
    private readonly FileIO _fileIO;
    private readonly AutoFixLlmAnswer _autoFixLlmAnswer;
    private readonly DirectoryPacker _directoryPacker;
    private readonly ILogger<CodeAgent> _logger;

    public async Task<bool> Run(string? mainInstruction)
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

        var fileContent = _directoryPacker.GetFileContent(targetFilePath);

        var extractedTypes = await _externalTypesFromInstructionsAgent.Run(codeOptions.TypesFromInstructionsPrompt, fileContent);
        var externalTypes = _codeTools.GetExternalTypes(codeLangugage, fileContent);
        var extractedTypesPaths = _codeTools.GetExistingPathsOfExternalTypes(allFiles, extractedTypes);
        var externalTypesPaths = _codeTools.GetExistingPathsOfExternalTypes(allFiles, externalTypes);

        List<string> additionalFileContents = [.. extractedTypesPaths.Concat(externalTypesPaths)];
        var filtered = additionalFileContents.Distinct();

        var additionalContext = _directoryPacker.PackFiles([.. filtered]);

        _conversation.AddMessage(ChatRole.System, codeOptions.SystemPrompt);
        _conversation.AddMessage(ChatRole.User, codeOptions.UserPrompt, new { mainInstruction, additionalContext, input = fileContent });

        await _conversation.CompleteAsync([GetExternalTypeImplementation]);
        var answer = await _autoFixLlmAnswer.RetrieveCodeFragment(_conversation, IsCodeOnly, codeOptions.RegeneratePrompt);
        if (answer is null) return false;

        var codeOnly = answer[codeOptions.Prefix.Length..^codeOptions.Postfix.Length];

        await _fileIO.WriteAsync(targetFilePath, codeOnly);

        return true;

        bool IsCodeOnly(string result) => result.StartsWith(codeOptions.Prefix.Trim()) && result.EndsWith(codeOptions.Postfix.Trim());

        [Description("Get the extrnal type implementation.")]
        string GetExternalTypeImplementation(string typeName)
        {
            return string.Empty;
            // _logger.LogInformation("Getting external type implementation for {typeName}.", typeName);
            // var result = await _codeTools.GetContentOfExternalTypes(allFiles, [typeName]);
            // return result.FirstOrDefault() ?? string.Empty;
        }
    }
}

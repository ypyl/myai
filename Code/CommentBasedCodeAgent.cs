using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using MyAi.Tools;

namespace MyAi.Code;

public class CommentBasedCodeAgent
{
    public CommentBasedCodeAgent(ExternalProcess externalProcess, VSCode VSCode, CodeTools codeTools, WorkingDirectory workingDirectory,
        ExternalTypesFromCodeCommentsAgent externalTypesFromCodeCommentsAgent, Conversation conversation, FileIO fileIO,
        AutoFixLlmAnswer autoFixLlmAnswer, DirectoryPacker directoryPacker, ILogger<CommentBasedCodeAgent> logger)
    {
        _externalProcess = externalProcess;
        _vsCode = VSCode;
        _codeTools = codeTools;
        _workingDirectory = workingDirectory;
        _externalTypesFromCodeCommentsAgent = externalTypesFromCodeCommentsAgent;
        _conversation = conversation;
        _fileIO = fileIO;
        _autoFixLlmAnswer = autoFixLlmAnswer;
        _directoryPacker = directoryPacker;
        _logger = logger;
    }

    private readonly ExternalProcess _externalProcess;

    private readonly VSCode _vsCode;

    private readonly CodeTools _codeTools;
    private readonly WorkingDirectory _workingDirectory;
    private readonly ExternalTypesFromCodeCommentsAgent _externalTypesFromCodeCommentsAgent;
    private readonly Conversation _conversation;
    private readonly FileIO _fileIO;
    private readonly AutoFixLlmAnswer _autoFixLlmAnswer;
    private readonly DirectoryPacker _directoryPacker;
    private readonly ILogger<CommentBasedCodeAgent> _logger;

    public async Task<bool> Run()
    {
        var targetWindowTitle = _externalProcess.GetFocusedWindowTitle();

        var targetFileName = _vsCode.ParseWindowTitle(targetWindowTitle);
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

        var extractedTypes = await _externalTypesFromCodeCommentsAgent.Run(codeOptions.TypesFromCodeCommentsPrompt, codeOptions.TypesFromCodeCommentsPromptUserPrompt, fileContent);
        var externalTypes = _codeTools.GetExternalTypes(codeLangugage, fileContent);
        var extractedTypesPaths = _codeTools.GetExistingPathsOfExternalTypes(allFiles, extractedTypes);
        var externalTypesPaths = _codeTools.GetExistingPathsOfExternalTypes(allFiles, externalTypes);

        List<string> additionalFileContents = [.. extractedTypesPaths.Concat(externalTypesPaths)];
        var filtered = additionalFileContents.Distinct();

        var additionalContext = _directoryPacker.PackFiles([.. filtered]);

        _conversation.AddMessage(ChatRole.System, codeOptions.CommentBasedCodeSystemPrompt);
        _conversation.AddMessage(ChatRole.User, codeOptions.CommentBasedCodeUserPrompt, new { additionalContext, input = fileContent });

        await _conversation.CompleteAsync([GetExternalTypeImplementation]);
        var answer = await _autoFixLlmAnswer.RetrieveCodeFragment(_conversation, IsCodeOnly, codeOptions.RegeneratePrompt);
        if (answer is null) return false;

        var codeOnly = answer[codeOptions.Prefix.Length..^codeOptions.Postfix.Length];

        await _fileIO.WriteAsync(targetFilePath, codeOnly);

        return true;

        bool IsCodeOnly(string result) => result.StartsWith(codeOptions.Prefix.Trim()) && result.EndsWith(codeOptions.Postfix.Trim());

        [Description("Get the implementation of the class based on its name.")]
        string GetExternalTypeImplementation(string className)
        {
            var result = _codeTools.GetExistingPathsOfExternalTypes(allFiles, [className]);
            if (result.Count == 0) return "No implementation found.";
            return _directoryPacker.GetFileContent(result.First());
        }
    }
}

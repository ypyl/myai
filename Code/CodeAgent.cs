using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using MyAi.Tools;

namespace MyAi.Code;

public class CodeAgent
{
    public CodeAgent(ExternalProcess externalProcess, VSCode VSCode, CodeTools codeTools, WorkingDirectory workingDirectory,
        Conversation conversation, FileIO fileIO, AutoFixLlmAnswer autoFixLlmAnswer, DirectoryPacker directoryPacker, ILogger<CommentBasedCodeAgent> logger)
    {
        _externalProcess = externalProcess;
        _vsCode = VSCode;
        _codeTools = codeTools;
        _workingDirectory = workingDirectory;
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
    private readonly Conversation _conversation;
    private readonly FileIO _fileIO;
    private readonly AutoFixLlmAnswer _autoFixLlmAnswer;
    private readonly DirectoryPacker _directoryPacker;
    private readonly ILogger<CommentBasedCodeAgent> _logger;

    public async Task<bool> Run(string instruction)
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

        _conversation.AddMessage(ChatRole.System, codeOptions.InstructionBasedCodePrompts[0]);
        _conversation.AddMessage(ChatRole.User, codeOptions.InstructionBasedCodePrompts[1], new { input = fileContent, instruction });

        await _conversation.CompleteAsync([GetClassImplementation]);
        var answer = await _autoFixLlmAnswer.RetrieveCodeFragment(_conversation, IsCodeOnly, codeOptions.RegeneratePrompt);
        if (answer is null) return false;

        var codeOnly = answer[codeOptions.Prefix.Length..^codeOptions.Postfix.Length];

        await _fileIO.WriteAsync(targetFilePath, codeOnly);

        return true;

        bool IsCodeOnly(string result) => result.StartsWith(codeOptions.Prefix.Trim()) && result.EndsWith(codeOptions.Postfix.Trim());

        [Description("Get the implementation of the class based on its name.")]
        string GetClassImplementation(string className)
        {
            var cleanedClassName = className.Split(".")[^1].Trim();
            var result = _codeTools.GetExistingPathsOfExternalTypes(allFiles, [cleanedClassName]);
            if (result.Count == 0) return "No implementation found.";
            return _directoryPacker.GetFileContent(result.First());
        }
    }
}

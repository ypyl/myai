using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using MyAi.Tools;

namespace MyAi.Code;

public class CodeAgent
{
    public CodeAgent(ExternalProcess externalProcess, VSCode VSCode, CodeTools codeTools, WorkingDirectory workingDirectory,
        Conversation conversation, FileIO fileIO, AutoFixLlmAnswer autoFixLlmAnswer, DirectoryPacker directoryPacker, ILogger<CodeAgent> logger)
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
    private readonly ILogger<CodeAgent> _logger;

    public async Task<bool> Run(string? instruction)
    {
        var targetWindowTitle = _externalProcess.GetFocusedWindowTitle();

        var targetFileName = _vsCode.ParseWindowTitle(targetWindowTitle);
        var workingDir = _workingDirectory.GetWorkingDirectory();
        var codeLangugage = _codeTools.GetCodeLanguage(targetFileName);
        var codeOptions = _codeTools.GetCodeOptions(codeLangugage);

        var systemPrompt = instruction is null ? codeOptions.CommentBasedCodePrompts[0] : codeOptions.InstructionBasedCodePrompts[0];
        var userPrompt = instruction is null ? codeOptions.CommentBasedCodePrompts[1] : codeOptions.InstructionBasedCodePrompts[1];

        var allFiles = _codeTools.FindFilesByLanguage(workingDir, codeLangugage);
        if (!allFiles.TryGetValue(Path.GetFileNameWithoutExtension(targetFileName), out var targetFilePath))
        {
            _logger.LogError("Not able to find {targetFileName} in working directory.", targetFileName);
            return false;
        }

        var fileContent = _directoryPacker.GetFileContent(targetFilePath);

        var typesFromInstruction = ExtractAtWords(instruction is null ? fileContent : instruction);
        var additionalFilePaths = _codeTools.GetExistingPathsOfExternalTypes(allFiles, typesFromInstruction);

        var externalTypes = _codeTools.GetExternalTypes(codeLangugage, fileContent);
        var externalTypesPaths = _codeTools.GetExistingPathsOfExternalTypes(allFiles, externalTypes);
        additionalFilePaths = [.. additionalFilePaths.Concat(externalTypesPaths)];

        var filtered = additionalFilePaths.Distinct();

        var additionalContext = _directoryPacker.PackFiles([.. filtered]);

        _conversation.AddMessage(ChatRole.System, systemPrompt);
        _conversation.AddMessage(ChatRole.User, userPrompt, new { input = fileContent, instruction, additionalContext });

        await _conversation.CompleteAsync([GetSourceCodeByTypeName]);
        var answer = await _autoFixLlmAnswer.RetrieveCodeFragment(_conversation, IsCodeOnly, codeOptions.RegeneratePrompt);
        if (answer is null) return false;

        var codeOnly = answer[codeOptions.Prefix.Length..^codeOptions.Postfix.Length];

        await _fileIO.WriteAsync(targetFilePath, codeOnly);

        return true;

        bool IsCodeOnly(string result) => result.StartsWith(codeOptions.Prefix.Trim()) && result.EndsWith(codeOptions.Postfix.Trim());

        [Description("Retrieves the source code of the specified type or class by its name, providing implementation details necessary for accurate code modifications or references.")]
        string GetSourceCodeByTypeName(string className)
        {
            var cleanedClassName = className.Split(".")[^1].Trim();
            var result = _codeTools.GetExistingPathsOfExternalTypes(allFiles, [cleanedClassName]);
            if (result.Count == 0) return "No implementation found.";
            return _directoryPacker.GetFileContent(result.First());
        }
    }

    public List<string> ExtractAtWords(string instruction)
    {
        return [.. instruction.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(word => word.StartsWith('@')).Select(word => word.TrimStart('@'))];
    }
}

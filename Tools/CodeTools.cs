using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MyAi.Tools;

public sealed class CodeOptions
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string TypesFromInstructionsPrompt { get; set; } = string.Empty;
    public string RegeneratePrompt { get; set; } = string.Empty;
}

public enum CodeLanguage
{
    CSharp,
    Typescript,
}

public class CodeTools
{
    private readonly IConfiguration _configuration;
    private readonly FileFinder _fileFinder;
    private readonly CsNonStandardTypeExtractorPlugin _csNonStandardTypeExtractorPlugin;
    private readonly TsNonStandardModuleExtractorPlugin _tsNonStandardModuleExtractorPlugin;
    private readonly FileIO _fileIO;
    private readonly ILogger<CodeTools> _logger;

    public CodeTools(IConfiguration configuration, FileFinder fileFinder, CsNonStandardTypeExtractorPlugin csNonStandardTypeExtractorPlugin,
        TsNonStandardModuleExtractorPlugin tsNonStandardModuleExtractorPlugin, FileIO fileIO, ILogger<CodeTools> logger)
    {
        _configuration = configuration;
        _fileFinder = fileFinder;
        _csNonStandardTypeExtractorPlugin = csNonStandardTypeExtractorPlugin;
        _tsNonStandardModuleExtractorPlugin = tsNonStandardModuleExtractorPlugin;
        _fileIO = fileIO;
        _logger = logger;
    }

    public CodeLanguage GetCodeLanguage(string filePath)
    {
        _logger.LogInformation("Determining code language for file: {FilePath}", filePath);
        return Path.GetExtension(filePath) switch
        {
            ".cs" => CodeLanguage.CSharp,
            ".ts" => CodeLanguage.Typescript,
            ".tsx" => CodeLanguage.Typescript,
            _ => throw new NotSupportedException($"File extension not supported: {Path.GetExtension(filePath)}"),
        };
    }

    public CodeOptions GetCodeOptions(CodeLanguage language)
    {
        var sectionName = Enum.GetName(language) ?? throw new InvalidOperationException($"Unsupported code language: {language}");
        _logger.LogInformation("Retrieving CodeOptions for language: {Language}", sectionName);
        var codeOptions = _configuration.GetRequiredSection("Code").GetRequiredSection(sectionName).Get<CodeOptions>()
            ?? throw new InvalidOperationException($"Failed to retrieve CodeOptions for section: {sectionName}");
        return codeOptions;
    }

    public Dictionary<string, string> FindFilesByLanguage(string workingDir, CodeLanguage codeLangugage)
    {
        _logger.LogInformation("Finding files in directory: {WorkingDir} for language: {Language}", workingDir, codeLangugage);
        return codeLangugage switch
        {
            CodeLanguage.CSharp => _fileFinder.FindCsFiles(workingDir),
            CodeLanguage.Typescript => _fileFinder.FindTsFiles(workingDir),
            _ => throw new NotSupportedException($"Code language not supported: {codeLangugage}"),
        };
    }

    public List<string> GetExternalTypes(CodeLanguage codeLanguage, string targetFileContent)
    {
        _logger.LogInformation("Extracting external types from the file.");
        return codeLanguage switch
        {
            CodeLanguage.CSharp => _csNonStandardTypeExtractorPlugin.ExtractNonStandardTypes(targetFileContent),
            CodeLanguage.Typescript => _tsNonStandardModuleExtractorPlugin.ExtractNonStandardModules(targetFileContent),
            _ => new List<string>()
        };
    }

    public async Task<List<string>> GetContentOfExternalTypes(IDictionary<string, string> allFiles, List<string> externalTypes)
    {
        _logger.LogInformation("Getting content of external type files: {externalTypes}", string.Join(", ", externalTypes));
        var result = new List<string>();
        foreach (var path in allFiles.Where(x => externalTypes.Contains(x.Key)).Select(x => x.Value))
        {
            _logger.LogInformation("Reading content from file: {Path}", path);
            var content = await _fileIO.ReadAsync(path);
            result.Add(content);
        }
        return result;
    }
}

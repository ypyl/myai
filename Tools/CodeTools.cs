using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MyAi.Tools;

public sealed class CodeOptions
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string InputPrompt { get; set; } = string.Empty;
    public string AdditionalPrompt { get; set; } = string.Empty;
    public string TypesFromInstructionsPrompt { get; set; } = string.Empty;
    public string RegeneratePrompt { get; set; } = string.Empty;
}

public enum CodeLanguage
{
    CSharp,
    Typescript,
}

public class CodeTools(IConfiguration configuration, FileFinder fileFinder, CsNonStandardTypeExtractorPlugin csNonStandardTypeExtractorPlugin,
    TsNonStandardModuleExtractorPlugin tsNonStandardModuleExtractorPlugin, FileIO fileIO, ILogger<CodeTools> logger)
{
    public CodeLanguage GetCodeLanguage(string filePath)
    {
        logger.LogInformation("Determining code language for file: {FilePath}", filePath);
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
        logger.LogInformation("Retrieving CodeOptions for language: {Language}", sectionName);
        var codeOptions = configuration.GetRequiredSection("Code").GetRequiredSection(sectionName).Get<CodeOptions>()
            ?? throw new InvalidOperationException($"Failed to retrieve CodeOptions for section: {sectionName}");
        return codeOptions;
    }

    public Dictionary<string, string> FindFilesByLanguage(string workingDir, CodeLanguage codeLangugage)
    {
        logger.LogInformation("Finding files in directory: {WorkingDir} for language: {Language}", workingDir, codeLangugage);
        return codeLangugage switch
        {
            CodeLanguage.CSharp => fileFinder.FindCsFiles(workingDir),
            CodeLanguage.Typescript => fileFinder.FindTsFiles(workingDir),
            _ => throw new NotSupportedException($"Code language not supported: {codeLangugage}"),
        };
    }

    public List<string> GetExternalTypes(CodeLanguage codeLanguage, string targetFileContent)
    {
        logger.LogInformation("Extracting external types for language: {CodeLanguage}", codeLanguage);
        return codeLanguage switch
        {
            CodeLanguage.CSharp => csNonStandardTypeExtractorPlugin.ExtractNonStandardTypes(targetFileContent),
            CodeLanguage.Typescript => tsNonStandardModuleExtractorPlugin.ExtractNonStandardModules(targetFileContent),
            _ => []
        };
    }

    public async Task<List<string>> GetContentOfExternalTypes(IDictionary<string, string> allFiles, List<string> externalTypes)
    {
        logger.LogInformation("Getting content of external types");
        var result = new List<string>();
        foreach (var path in allFiles.Where(x => externalTypes.Contains(x.Key)).Select(x => x.Value))
        {
            logger.LogInformation("Reading content from file: {Path}", path);
            var content = await fileIO.ReadAsync(path);
            result.Add(content);
        }
        return result;
    }
}

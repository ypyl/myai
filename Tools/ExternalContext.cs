using Spectre.Console;

namespace MyAi.Tools;

public class ExternalContext(FileIO fileIO, CsNonStandardTypeExtractorPlugin csNonStandardTypeExtractorPlugin, TsNonStandardModuleExtractorPlugin tsNonStandardModuleExtractorPlugin)
{
    public async Task<List<string>> GetFiles(IDictionary<string, string> allFiles, string targetFile)
    {
        AnsiConsole.MarkupLine("[fuchsia]Extracting external types from the target code.[/]");

        var externalTypes = Path.GetExtension(targetFile) switch
        {
            ".cs" => csNonStandardTypeExtractorPlugin.ExtractNonStandardTypes(targetFile),
            ".ts" => tsNonStandardModuleExtractorPlugin.ExtractNonStandardModules(targetFile),
            ".tsx" => tsNonStandardModuleExtractorPlugin.ExtractNonStandardModules(targetFile),
            _ => []
        };
        var result = new List<string>();
        foreach (var path in allFiles.Where(x => externalTypes.Contains(x.Key)).Select(x => x.Value))
        {
            var content = await fileIO.ReadAsync(path);
            AnsiConsole.MarkupLine("[green]External context[/]: [navy]{0}[/]", path);
            result.Add(content);
        }
        return result;
    }
}

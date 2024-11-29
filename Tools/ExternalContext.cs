using Spectre.Console;

public class ExternalContext(FileIO fileIO)
{
    public async Task<List<string>> GetFiles(IDictionary<string, string> allFiles, string targetFile)
    {
        AnsiConsole.MarkupLine("[fuchsia]Extracting external types from the target code.[/]");

        var externalTypes = Path.GetExtension(targetFile) switch
        {
            ".cs" => new CsNonStandardTypeExtractorPlugin().ExtractNonStandardTypes(targetFile),
            ".ts" => new TsNonStandardModuleExtractorPlugin().ExtractNonStandardModules(targetFile),
            ".tsx" => new TsNonStandardModuleExtractorPlugin().ExtractNonStandardModules(targetFile),
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

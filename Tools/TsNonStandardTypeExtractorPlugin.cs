using System.Text.RegularExpressions;
using Spectre.Console;

namespace MyAi.Tools;

public sealed class TsNonStandardModuleExtractorPlugin
{
    public List<string> ExtractNonStandardModules(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            AnsiConsole.MarkupLine("[red]Error: Input code cannot be null or empty.[/]");
            return [];
        }
        return AnsiConsole.Status().Start("Searching non standard modules...", ctx =>
        {
            var pattern = @"from\s+['""]\.\/.*?\/(.*?)['""]";
            var matches = Regex.Matches(code, pattern);
            return matches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();
        });
    }
}

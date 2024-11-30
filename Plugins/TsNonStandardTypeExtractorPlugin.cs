using System.ComponentModel;
using System.Text.RegularExpressions;
using Spectre.Console;

[Description("Plugin to extract non-standard modules from Typescript code.")]
internal sealed class TsNonStandardModuleExtractorPlugin
{
    [Description("Extracts non-standard modules from the provided Typescript code string.")]
    [return: Description("List of non-standard module names used in the provided Typescript code.")]
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

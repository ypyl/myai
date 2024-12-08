using System.Text.RegularExpressions;
using Spectre.Console;

namespace MyAi.Code;

public sealed class TsNonStandardModuleExtractorPlugin
{
    public List<string> ExtractNonStandardModules(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return [];
        }
        var pattern = @"from\s+['""]\.\/.*?\/(.*?)['""]";
        var matches = Regex.Matches(code, pattern);
        return matches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();
    }
}

using Spectre.Console;

namespace MyAi.Tools;
public partial class PromptBuilder
{
    public string CreatePrompt(string template, params string[] parameters)
    {
        return AnsiConsole.Status().Start("Rendering prompt...", (ctx) =>
        {
            var result = template;
            foreach (var value in parameters)
            {
                result = AnyKeyRegex().Replace(result, value?.ToString() ?? string.Empty);
            }
            return result;
        });
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\{\{\s*\$\w+\s*\}\}")]
    private static partial System.Text.RegularExpressions.Regex AnyKeyRegex();
}

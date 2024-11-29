using Spectre.Console;

internal sealed class PromptBuilder
{
    public string CreatePrompt(string template, IDictionary<string, object?> parameters)
    {
        return AnsiConsole.Status().Start("Rendering prompt...", (ctx) =>
        {
            var result = template;
            foreach (var (key, value) in parameters)
            {
                result = result.Replace($"{{{{ ${key} }}}}", value?.ToString() ?? string.Empty);
            }
            return result;
        });
    }
}

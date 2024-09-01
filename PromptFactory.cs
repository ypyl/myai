using Microsoft.SemanticKernel;
using Spectre.Console;

internal sealed class PromptFactory
{
    private static readonly KernelPromptTemplateFactory _promptTemplateFactory = new();

    public static async Task<string> RenderPrompt(string template, IDictionary<string, object?> parameters)
    {
        return await AnsiConsole.Status().StartAsync("Rendering prompt...", async ctx =>
        {
            var arguments = new KernelArguments(parameters);
            var prompt = await _promptTemplateFactory.Create(new PromptTemplateConfig(template)).RenderAsync(new Kernel(), arguments);
            if (Env.Debug) AnsiConsole.MarkupLine($"[green]template[/]: {prompt.EscapeMarkup()}");
            return prompt;
        });
    }
}

using Microsoft.SemanticKernel;
using Serilog;
using Spectre.Console;

internal sealed class PromptFactory(ILogger logger)
{
    private readonly KernelPromptTemplateFactory _promptTemplateFactory = new();

    public async Task<string> RenderPrompt(string template, IDictionary<string, object?> parameters)
    {
        return await AnsiConsole.Status().StartAsync("Rendering prompt...", async ctx =>
        {
            var arguments = new KernelArguments(parameters);
            logger.Verbose(nameof(RenderPrompt));
            logger.Verbose("template:");
            logger.Verbose(template);
            var prompt = await _promptTemplateFactory.Create(new PromptTemplateConfig(template)).RenderAsync(new Kernel(), arguments);

            logger.Verbose("prompt:");
            logger.Verbose(prompt);
            return prompt;
        });
    }
}

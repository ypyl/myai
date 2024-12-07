using Stubble.Core.Interfaces;

namespace MyAi.Tools;
public class PromptBuilder
{
    private readonly IStubbleRenderer _stubbleRenderer;

    public PromptBuilder(IStubbleRenderer stubbleRenderer)
    {
        _stubbleRenderer = stubbleRenderer;
    }
    public string CreatePrompt(string template, object viewModel)
    {
        return _stubbleRenderer.Render(template, viewModel);
    }
}

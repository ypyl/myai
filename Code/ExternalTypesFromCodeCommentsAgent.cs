using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MyAi.Code;

public class ExternalTypesFromCodeCommentsAgent
{
    private readonly Conversation _conversation;
    private readonly ILogger<ExternalTypesFromCodeCommentsAgent> _logger;

    public ExternalTypesFromCodeCommentsAgent(Conversation conversation, ILogger<ExternalTypesFromCodeCommentsAgent> logger)
    {
        _conversation = conversation;
        _logger = logger;
    }
    public async Task<List<string>> Run(List<string> prompts, string targetFileContent)
    {
        _logger.LogInformation("Getting external types from code comments.");
        _conversation.AddMessage(ChatRole.System, prompts[0]);
        _conversation.AddMessage(ChatRole.User, prompts[1], new { input = targetFileContent });

        await _conversation.CompleteAsync();

        var txtAnswer = _conversation.LLMResponse ?? string.Empty;
        _logger.LogInformation("LLM Response: {txtAnswer}", string.IsNullOrWhiteSpace(txtAnswer) ? "EMPTY RESPONSE" : txtAnswer);
        var typesFromInstructions = txtAnswer.Split(["*", "-", "\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries).Select(type => type.Trim());
        return typesFromInstructions.ToList() ?? [];
    }
}

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MyAi.Code;

public class ExternalTypesFromInstructionsAgent
{
    private readonly Conversation _conversation;
    private readonly ILogger<ExternalTypesFromInstructionsAgent> _logger;

    public ExternalTypesFromInstructionsAgent(Conversation conversation, ILogger<ExternalTypesFromInstructionsAgent> logger)
    {
        _conversation = conversation;
        _logger = logger;
    }
    public async Task<List<string>> Run(string typesFromInstructionsPrompt, string targetFileContent)
    {
        _logger.LogInformation("Getting external types from instructions.");
        _conversation.AddMessage(ChatRole.System, typesFromInstructionsPrompt);
        _conversation.AddMessage(ChatRole.User, targetFileContent);

        await _conversation.CompleteAsync();

        var txtAnswer = _conversation.LLMResponse ?? string.Empty;
        _logger.LogInformation("LLM Response: {txtAnswer}", string.IsNullOrWhiteSpace(txtAnswer) ? "EMPTY RESPONSE" : txtAnswer);
        var typesFromInstructions = txtAnswer.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        return typesFromInstructions.ToList() ?? [];
    }
}

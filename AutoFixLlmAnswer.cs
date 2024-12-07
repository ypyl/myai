using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MyAi;

public class AutoFixLlmAnswer
{
    private readonly ILogger<AutoFixLlmAnswer> _logger;

    public AutoFixLlmAnswer(ILogger<AutoFixLlmAnswer> logger)
    {
        _logger = logger;
    }
    public async Task<string?> RetrieveCodeFragment(Conversation conversation, Func<string, bool> check,
        string regeneratePrompt)
    {
        var maxAttemtp = 3;
        var answer = conversation.LLMResponse ?? string.Empty;

        while (!check(answer) && maxAttemtp-- > 0)
        {
            _logger.LogInformation("Answer does not meet the requirements. Regenerating the answer. Attempt: {Attempt}", 3 - maxAttemtp);
            conversation.AddMessage(ChatRole.User, regeneratePrompt);
            await conversation.CompleteAsync();

            answer = conversation.LLMResponse ?? string.Empty;
        }

        if (maxAttemtp == 0) return null;

        return answer;
    }
}

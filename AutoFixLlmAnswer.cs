using Microsoft.Extensions.AI;

namespace MyAi;

public class AutoFixLlmAnswer
{
    public async Task<string?> RetrieveCodeFragment(Conversation conversation, Func<string, bool> check,
        string regeneratePrompt)
    {
        var maxAttemtp = 3;
        var answer = conversation.LLMResponse ?? string.Empty;

        while (!check(answer) && maxAttemtp-- > 0)
        {
            conversation.AddMessage(ChatRole.User, regeneratePrompt);
            await conversation.CompleteAsync();

            answer = conversation.LLMResponse ?? string.Empty;
        }

        if (maxAttemtp == 0) return null;

        return answer;
    }
}

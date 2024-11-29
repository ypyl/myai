

using Microsoft.Extensions.AI;
using Serilog;
using Spectre.Console;

internal sealed class Conversation(string systemPrompt, IChatCompletionService chatCompletionService, ILogger logger)
{
    private readonly List<ChatMessage> _chatHistory = [];

    public void AddSystemMessage(string message)
    {
        _chatHistory.Add(new ChatMessage(ChatRole.System, message));
    }

    public async Task<string> Say(string message)
    {
        _chatHistory.AddUserMessage(message);
        MessageOutputAsync(_chatHistory);
        var reply = await AnsiConsole.Status().StartAsync("Waiting model answer...", async ctx =>
        {
            return await chatCompletionService.GetChatMessageContentAsync(_chatHistory);
        });
        _chatHistory.Add(reply);
        MessageOutputAsync(_chatHistory);
        return reply.ToString();
    }

    private void MessageOutputAsync(ChatHistory chatHistory)
    {
        var message = chatHistory.Last();
        logger.Verbose($"{message.Role}: {message.Content}");
    }
}

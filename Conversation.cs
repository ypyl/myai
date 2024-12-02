using System.Collections.ObjectModel;
using Microsoft.Extensions.AI;
using MyAi.Tools;

namespace MyAi;

public class Conversation(PromptBuilder promptBuilder, IChatClient chatClient)
{
    private readonly List<ChatMessage> _chatHistory = [];

    public ReadOnlyCollection<ChatMessage> ChatHistory => _chatHistory.AsReadOnly();

    public void AddMessage(ChatMessage message) => _chatHistory.Add(message);

    public void AddMessage(ChatRole role, string text) => _chatHistory.Add(new(role, text));
    public void AddMessage(ChatRole role, string prompt, params string[] parameters) =>
        _chatHistory.Add(new(role, promptBuilder.CreatePrompt(prompt, parameters)));

    public string? LLMResponse => _chatHistory.Last().Text;

    public async Task<ChatMessage> CompleteAsync()
    {
        var response = await chatClient.CompleteAsync(_chatHistory);
        _chatHistory.Add(response.Message);
        return response.Message;
    }
}

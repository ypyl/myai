
using System.Collections.ObjectModel;
using Microsoft.Extensions.AI;

namespace MyAi;

public interface IConversation
{
    ReadOnlyCollection<ChatMessage> ChatHistory { get; }
    void AddMessage(ChatMessage message);
    void AddMessage(ChatRole role, string text);
    void AddMessage(ChatRole role, string prompt, params string[] parameters);
    string? LLMResponse { get; }
    Task<ChatMessage> CompleteAsync();
}

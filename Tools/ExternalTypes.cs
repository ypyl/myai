using Microsoft.Extensions.AI;
using Spectre.Console;

public class ExternalTypes(FileIO fileIO, PromptBuilder promptBuilder, IChatClient chatClient)
{
    protected async Task<List<string>> ExternalTypesFromInstructionContext(string system, string promptTypesFromInstructions, IDictionary<string, string> allFiles, string targetFileContent)
    {
        AnsiConsole.MarkupLine("[fuchsia]Getting external types from instructions.[/]");
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, system),
            new(ChatRole.User, promptBuilder.CreatePrompt(promptTypesFromInstructions,
                new Dictionary<string, object?> { ["code"] = targetFileContent })),
        };

        var answer = await chatClient.CompleteAsync(messages);
        var typesFromInstructions = answer.Message.Text?.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries) ?? [];
        var result = new List<string>();
        foreach (var path in allFiles.Where(x => typesFromInstructions.Contains(x.Key)).Select(x => x.Value))
        {
            var content = await fileIO.ReadAsync(path);
            AnsiConsole.MarkupLine("[green]External context[/]: [navy]{0}[/]", path);
            result.Add(content);
        }
        return result;
    }
}

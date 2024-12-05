using Microsoft.Extensions.AI;
using Spectre.Console;

namespace MyAi.Code;

public class ExternalTypesFromInstructionContext(Conversation conversation)
{
    public async Task<List<string>> Extract(string typesFromInstructionsPrompt, IDictionary<string, string> allFiles, string targetFileContent)
    {
        AnsiConsole.MarkupLine("[fuchsia]Getting external types from instructions.[/]");
        conversation.AddMessage(ChatRole.System, typesFromInstructionsPrompt, targetFileContent);

        await conversation.CompleteAsync();

        var txtAnswer = conversation.LLMResponse ?? string.Empty;
        var typesFromInstructions = txtAnswer.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        foreach (var path in allFiles.Where(x => typesFromInstructions.Contains(x.Key)).Select(x => x.Value))
        {
            var content = await new FileIO().ReadAsync(path);
            AnsiConsole.MarkupLine("[green]External context[/]: [navy]{0}[/]", path);
            result.Add(content);
        }
        return result;
    }
}

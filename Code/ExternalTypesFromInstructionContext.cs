using Microsoft.Extensions.AI;
using MyAi.Tools;
using Spectre.Console;

namespace MyAi.Code;

public class ExternalTypesFromInstructionContext(PromptBuilder promptBuilder, IChatClient chatClient)
{
    public async Task<List<string>> Extract(CodeOptions codeOptions, IDictionary<string, string> allFiles, string targetFileContent)
    {
        AnsiConsole.MarkupLine("[fuchsia]Getting external types from instructions.[/]");
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, promptBuilder.CreatePrompt(codeOptions.TypesFromInstructionsPrompt, targetFileContent)),
        };

        var answer = await chatClient.CompleteAsync(messages);

        var txtAnswer = answer.Message.Text ?? string.Empty;
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

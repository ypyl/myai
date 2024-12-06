using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace MyAi.Code;

public class ExternalTypesFromInstructionsAgent(Conversation conversation, ILogger<ExternalTypesFromInstructionsAgent> logger)
{
    public async Task<List<string>> Run(string typesFromInstructionsPrompt, IDictionary<string, string> allFiles, string targetFileContent)
    {
        logger.LogInformation("Getting external types from instructions.");
        conversation.AddMessage(ChatRole.System, typesFromInstructionsPrompt, targetFileContent);

        await conversation.CompleteAsync();

        var txtAnswer = conversation.LLMResponse ?? string.Empty;
        logger.LogInformation("LLM Response: {txtAnswer}", txtAnswer);
        var typesFromInstructions = txtAnswer.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        foreach (var path in allFiles.Where(x => typesFromInstructions.Contains(x.Key)).Select(x => x.Value))
        {
            var content = await new FileIO().ReadAsync(path);
            logger.LogTrace("External context: {path}", path);
            result.Add(content);
        }
        return result;
    }
}

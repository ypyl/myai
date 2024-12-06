using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Spectre.Console;

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
    public async Task<List<string>> Run(string typesFromInstructionsPrompt, IDictionary<string, string> allFiles, string targetFileContent)
    {
        _logger.LogInformation("Getting external types from instructions.");
        _conversation.AddMessage(ChatRole.System, typesFromInstructionsPrompt, targetFileContent);

        await _conversation.CompleteAsync();

        var txtAnswer = _conversation.LLMResponse ?? string.Empty;
        _logger.LogInformation("LLM Response: {txtAnswer}", txtAnswer);
        var typesFromInstructions = txtAnswer.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        foreach (var path in allFiles.Where(x => typesFromInstructions.Contains(x.Key)).Select(x => x.Value))
        {
            var content = await new FileIO().ReadAsync(path);
            _logger.LogTrace("External context: {path}", path);
            result.Add(content);
        }
        return result;
    }
}

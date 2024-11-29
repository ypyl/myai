// using Spectre.Console;
// using Spectre.Console.Cli;
// internal abstract class BaseCommand<T> : AsyncCommand<T> where T : CommandSettings
// {
//     protected ExternalProcess ExternalProcessPlugin { get; }
//     protected BaseCommand()
//     {
//         ExternalProcessPlugin = new ExternalProcessPlugin(_config.GetStringValue("$.process_name"), _config.GetIntValue("$.pid"));
//     }

//     protected async Task<List<string>> ExternalTypesFromInstructionContext(string promptTypesFromInstructions, IDictionary<string, string> allFiles, string targetFileContent, ILogger logger)
//     {
//         AnsiConsole.MarkupLine("[fuchsia]Getting external types from instructions.[/]");
//         var completionService = new CompletionService(_config).CreateChatCompletionService();
//         var conversation = new Conversation(_config.GetStringValue("$.system"), completionService, logger);
//         var userMessage = await new PromptBuilder(logger).CreatePrompt(promptTypesFromInstructions,
//             new Dictionary<string, object?> { ["code"] = targetFileContent });
//         var answer = await conversation.Say(userMessage);
//         var typesFromInstructions = answer.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
//         var result = new List<string>();
//         foreach (var path in allFiles.Where(x => typesFromInstructions.Contains(x.Key)).Select(x => x.Value))
//         {
//             var content = await new FileIO().ReadAsync(path);
//             AnsiConsole.MarkupLine("[green]External context[/]: [navy]{0}[/]", path);
//             result.Add(content);
//         }
//         return result;
//     }

//     // TODO refactor
//     protected async Task FixGeneratedOutput(Conversation conversation, string answer, Func<string, Task> action, string promptRegenerate, string prefix, string postfix)
//     {
//         var regenerate = true;
//         while (regenerate)
//         {
//             answer = await RetryGenerateCode(conversation, answer, promptRegenerate, prefix, postfix);
//             var codeOnly = answer[prefix.Length..^postfix.Length];

//             await action(codeOnly.TrimStart());

//             var userComment = AnsiConsole.Prompt(new TextPrompt<string>("[green]Anything to fix?[/]").AllowEmpty());

//             regenerate = !string.IsNullOrWhiteSpace(userComment);

//             if (!regenerate) break;

//             answer = await new PromptBuilder(Logger).CreatePrompt(promptRegenerate, new Dictionary<string, object?> { ["csharp_comment"] = userComment });
//         }
//     }

//     private static async Task<string> RetryGenerateCode(Conversation conversation, string answer, string promptRegenerate, string prefix, string postfix)
//     {
//         while (!answer.StartsWith(prefix) || !answer.EndsWith(postfix))
//         {
//             answer = await conversation.Say(string.Format(promptRegenerate, prefix, postfix));
//         }
//         return answer;
//     }
// }

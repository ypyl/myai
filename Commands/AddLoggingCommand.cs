// using System.Diagnostics.CodeAnalysis;
// using Microsoft.Extensions.AI;
// using Microsoft.Extensions.Options;
// using Spectre.Console;
// using Spectre.Console.Cli;

// internal sealed class AddLoggingCommand(IChatClient chatClient,
//     IOptions<AddLoggingOptions> options, PromptBuilder promptBuilder, ExternalProcess externalProcess,
//     FileFinder fileFinder, ExternalContext externalContext) : AsyncCommand<AddLoggingCommand.Settings>
// {
//     private string PromptMain => options.Value.System;
//     private string RegenerateAnswer => options.Value.RegenerateAnswer;
//     private string RegenerateFormat => options.Value.RegenerateFormat;

//     public sealed class Settings : CommandSettings
//     {
//     }

//     public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
//     {
//         var targetFileName = externalProcess.GetFocusedWindowTitle();
//         var workdingDir = string.Empty; // TODO

//         var allFiles = fileFinder.FindCsFiles(workdingDir);
//         if (!allFiles.TryGetValue(Path.GetFileNameWithoutExtension(targetFileName), out var targetFilePath))
//         {
//             AnsiConsole.MarkupLine("[red]Not able to find {0} in workding directory.[/]", targetFileName);
//             return 1;
//         }
//         var fileContent = await new FileIO().ReadAsync(targetFilePath);

//         var additional = await externalContext.GetFiles(allFiles, fileContent);

//         var filtered = additional.Distinct();

//         var userMessage = promptBuilder.CreatePrompt(PromptMain,
//             new Dictionary<string, object?> { ["csharp_code"] = fileContent, ["csharp_additional_code"] = string.Join("\n\n", filtered) });

//         var messages = new List<ChatMessage>
//         {
//             new(ChatRole.System, PromptMain),
//             new(ChatRole.User, userMessage),
//         };

//         var answer = await chatClient.CompleteAsync(messages);

//         const string Prefix = "```csharp";
//         const string Postfix = "```";

//         var regenerate = true;
//         var txtAnswer = answer.Message.Text ?? string.Empty;
//         messages.Add(answer.Message);
//         while (regenerate)
//         {
//             while (txtAnswer.StartsWith(Prefix) || !txtAnswer.EndsWith(Postfix))
//             {
//                 messages.Add(new(ChatRole.User, string.Format(RegenerateFormat, Prefix, Postfix)));
//                 answer = await chatClient.CompleteAsync(messages);
//             }
//             txtAnswer = answer.Message.Text ?? string.Empty;
//             var codeOnly = txtAnswer[Prefix.Length..^Postfix.Length];

//             await new FileIO().WriteAsync(targetFilePath, codeOnly.TrimStart());

//             var userComment = AnsiConsole.Prompt(new TextPrompt<string>("[green]Anything to fix?[/]").AllowEmpty());

//             regenerate = !string.IsNullOrWhiteSpace(userComment);

//             if (!regenerate) break;

//             messages.Add(new(ChatRole.User, promptBuilder.CreatePrompt(RegenerateAnswer,
//                 new Dictionary<string, object?> { ["csharp_comment"] = userComment })));

//             answer = await chatClient.CompleteAsync(messages);
//         }

//         return 0;
//     }
// }

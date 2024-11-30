
// using System.Diagnostics.CodeAnalysis;
// using Serilog;
// using Serilog.Core;
// using Spectre.Console;
// using Spectre.Console.Cli;

// internal sealed class JsonCommand : BaseCommand<JsonCommand.Settings>
// {
//     private string PromptMain => _config.GetStringValue("$.json.main");
//     private string PromptRegenerate => _config.GetStringValue("$.json.regenerate");

//     public sealed class Settings : CommandSettings
//     {

//     }

//     public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
//     {
//         var targetFileName = ExternalProcessPlugin.WindowTargetFileName();

//         var allFiles = new FileFinderPlugin(_config.GetStringValue("$.working_dir"), Logger).FindJsonFiles();
//         if (!allFiles.TryGetValue(Path.GetFileNameWithoutExtension(targetFileName), out var targetFilePath))
//         {
//             AnsiConsole.MarkupLine("[red]Not able to find {0} in workding directory.[/]", targetFileName);
//             return 1;
//         }
//         var fileContent = await new FileIO().ReadAsync(targetFilePath);

//         var userMessage = await new PromptBuilder(Logger).CreatePrompt(PromptMain,
//             new Dictionary<string, object?> { ["json_data"] = fileContent });

//         var completionService = new CompletionService(_config).CreateChatCompletionService();

//         var conversation = new Conversation(_config.GetStringValue("$.system"), completionService, Logger);

//         var answer = await conversation.Say(userMessage);
//         const string Prefix = "```json";
//         const string Postfix = "```";

//         async Task action(string code)
//         {
//             await new FileIO().WriteAsync(targetFilePath, code);
//         }

//         await FixGeneratedOutput(conversation, answer, action, PromptRegenerate, Prefix, Postfix);

//         return 0;
//     }
// }


using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

internal sealed class CodeCommand : BaseCommand<CodeCommand.Settings>
{
    enum CodeLanguage
    {
        CSharp,
        Typescript,
    }

    public sealed class Settings : CommandSettings
    {

    }

    private CodeLanguage GetCodeLanguage(string filePath)
    {
        return Path.GetExtension(filePath) switch
        {
            ".cs" => CodeLanguage.CSharp,
            ".ts" => CodeLanguage.Typescript,
            ".tsx" => CodeLanguage.Typescript,
            _ => throw new NotSupportedException(),
        };
    }

    record CodeSettings
    {
        public required string WorkingDirectory { get; init; }
        public required string TypesFromInstructions { get; init; }
        public required Func<FileFinder, IDictionary<string, string>> FindFiles { get; init; }
    }

    private CodeSettings GetCodeSettings(CodeLanguage language)
    {
        return language switch
        {
            CodeLanguage.CSharp => new()
            {
                WorkingDirectory = _config.GetStringValue("$.code.csharp.working_dir"),
                TypesFromInstructions = _config.GetStringValue("$.code.csharp.types_from_instructions"),
                FindFiles = (plugin) => plugin.FindCsFiles(),
            },
            CodeLanguage.Typescript => new()
            {
                WorkingDirectory = _config.GetStringValue("$.code.typescript.working_dir"),
                TypesFromInstructions = _config.GetStringValue("$.code.typescript.types_from_instructions"),
                FindFiles = (plugin) => plugin.FindTsFiles(),
            },
            _ => throw new NotSupportedException(),
        };
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
        Settings? settings = config.GetRequiredSection("Settings").Get<Settings>();
        var targetFileName = ExternalProcessPlugin.WindowTargetFileName();
        var codeLangugage = GetCodeLanguage(targetFileName);
        var codeSettings = GetCodeSettings(codeLangugage);
        var allFiles = codeSettings.FindFiles(new FileFinderPlugin(codeSettings.WorkingDirectory, Logger));
        if (!allFiles.TryGetValue(Path.GetFileNameWithoutExtension(targetFileName), out var targetFilePath))
        {
            AnsiConsole.MarkupLine("[red]Not able to find {0} in workding directory.[/]", targetFileName);
            return 1;
        }

        var fileContent = await new FileIO().ReadAsync(targetFilePath);

        if (Path.GetExtension(targetFileName) == ".cs")
        {
            var additionalFromInstruction = await ExternalTypesFromInstructionContext(_config.GetStringValue("$.code.csharp.types_from_instructions"), allFiles, fileContent, Logger);

            var additional = await ExternalContext(allFiles, fileContent);

            List<string> additionalFileContents = [.. additionalFromInstruction, .. additional];
            var filtered = additionalFileContents.Distinct();

            var additionalContext = filtered.Any() ?
                await new PromptBuilder(Logger).RenderPrompt(_config.GetStringValue("$.code.csharp.additional_context"),
                    new Dictionary<string, object?> { ["code"] = string.Join("\n\n", filtered) }) : string.Empty;

            if (!string.IsNullOrWhiteSpace(additionalContext))
            {
                additionalContext += "\n";
            }

            var userMessage = await new PromptBuilder(Logger).RenderPrompt(_config.GetStringValue("$.code.csharp.user_message_code"),
                new Dictionary<string, object?> { ["code"] = fileContent });

            var completionService = new CompletionService(_config).CreateChatCompletionService();

            var conversation = new Conversation(_config.GetStringValue("$.code.csharp.system"), completionService, Logger);

            var answer = await conversation.Say(additionalContext + userMessage);

            const string Prefix = "```csharp";
            const string Postfix = "```";

            async Task action(string code)
            {
                await new FileIO().WriteAsync(targetFilePath, code);
            }

            await FixGeneratedOutput(conversation, answer, action, _config.GetStringValue("$.code.regenerate"), Prefix, Postfix);

            return 0;
        }

        if (Path.GetExtension(targetFileName) == ".ts" || Path.GetExtension(targetFileName) == ".tsx")
        {
            var additionalFromInstruction = await ExternalTypesFromInstructionContext(_config.GetStringValue("$.code.typescript.types_from_instructions"), allFiles, fileContent, Logger);

            var additional = await ExternalContext(allFiles, fileContent);

            List<string> additionalFileContents = [.. additionalFromInstruction, .. additional];
            var filtered = additionalFileContents.Distinct();

            var additionalContext = filtered.Any() ?
                await new PromptBuilder(Logger).RenderPrompt(_config.GetStringValue("$.code.typescript.additional_context"),
                    new Dictionary<string, object?> { ["code"] = string.Join("\n\n", filtered) }) : string.Empty;

            if (!string.IsNullOrWhiteSpace(additionalContext))
            {
                additionalContext += "\n";
            }

            var userMessage = await new PromptBuilder(Logger).RenderPrompt(_config.GetStringValue("$.code.typescript.user_message_code"),
                new Dictionary<string, object?> { ["code"] = fileContent });

            var completionService = new CompletionService(_config).CreateChatCompletionService();

            var conversation = new Conversation(_config.GetStringValue("$.code.typescript.system"), completionService, Logger);

            var answer = await conversation.Say(additionalContext + userMessage);

            const string Prefix = "```typescript";
            const string Postfix = "```";

            async Task action(string code)
            {
                await new FileIO().WriteAsync(targetFilePath, code);
            }

            await FixGeneratedOutput(conversation, answer, action, _config.GetStringValue("$.code.regenerate"), Prefix, Postfix);

            return 0;
        }
        return 1;
    }
}

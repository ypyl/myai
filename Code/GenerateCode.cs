using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using MyAi.Tools;
using Spectre.Console;

namespace MyAi.Code;

public class GenerateCode(ExternalProcess externalProcess, FileFinder fileFinder, IConfiguration configuration, WorkingDirectory workingDirectory,
    ExternalTypesFromInstructionContext externalTypesFromInstructionContext, ExternalContext externalContext, Conversation conversation, FileIO fileIO,
    AutoFixLlmAnswer autoFixLlmAnswer)
{
    enum CodeLanguage
    {
        CSharp,
        Typescript,
    }

    private static CodeLanguage GetCodeLanguage(string filePath)
    {
        return Path.GetExtension(filePath) switch
        {
            ".cs" => CodeLanguage.CSharp,
            ".ts" => CodeLanguage.Typescript,
            ".tsx" => CodeLanguage.Typescript,
            _ => throw new NotSupportedException(),
        };
    }

    private CodeOptions? GetCodeOptions(CodeLanguage language)
    {
        var sectionName = Enum.GetName(language);
        if (sectionName is null)
        {
            return null;
        }
        return configuration.GetRequiredSection("Code").GetRequiredSection(sectionName).Get<CodeOptions>();
    }

    const string Prefix = "```csharp";
    const string Postfix = "```";

    public async Task<bool> Run()
    {
        var targetWindowTitle = externalProcess.GetFocusedWindowTitle();
        var targetFileName = externalProcess.GetFileName(targetWindowTitle);
        if (targetFileName is null)
        {
            AnsiConsole.MarkupLine("[red]Not able to find target file name.[/]");
            return false;
        }
        var workingDir = workingDirectory.GetWorkingDirectory();
        var codeLangugage = GetCodeLanguage(targetFileName);
        var codeOptions = GetCodeOptions(codeLangugage);
        if (codeOptions is null)
        {
            AnsiConsole.MarkupLine("[red]Not able to find code options for {0} language.[/]", codeLangugage);
            return false;
        }
        var allFiles = codeLangugage switch
        {
            CodeLanguage.CSharp => fileFinder.FindCsFiles(workingDir),
            CodeLanguage.Typescript => fileFinder.FindTsFiles(workingDir),
            _ => throw new NotSupportedException(),
        };
        if (!allFiles.TryGetValue(Path.GetFileNameWithoutExtension(targetFileName), out var targetFilePath))
        {
            AnsiConsole.MarkupLine("[red]Not able to find {0} in workding directory.[/]", targetFileName);
            return false;
        }

        var fileContent = await new FileIO().ReadAsync(targetFilePath);

        var additionalFromInstruction = await externalTypesFromInstructionContext.Extract(codeOptions, allFiles, fileContent);

        var additionalFromFile = await externalContext.GetFiles(allFiles, fileContent);

        List<string> additionalFileContents = [.. additionalFromInstruction, .. additionalFromFile];
        var filtered = additionalFileContents.Distinct();

        var additionalContext = string.Join("\n\n", filtered);

        conversation.AddMessage(ChatRole.System, codeOptions.SystemPrompt);
        conversation.AddMessage(ChatRole.User, codeOptions.InputPrompt, fileContent);

        if (!string.IsNullOrWhiteSpace(additionalContext))
        {
            conversation.AddMessage(ChatRole.User, codeOptions.AdditionalPrompt, additionalContext);
        }

        string? userComment;
        do
        {
            await conversation.CompleteAsync();
            var answer = await autoFixLlmAnswer.RetrieveCodeFragment(conversation, IsCodeOnly, codeOptions.RegeneratePrompt);
            if (answer is null) return false;
            var codeOnly = answer[Prefix.Length..^Postfix.Length];

            await fileIO.WriteAsync(targetFilePath, codeOnly);
            userComment = AnsiConsole.Prompt(new TextPrompt<string>("[green]Anything to fix?[/]").AllowEmpty());
            conversation.AddMessage(ChatRole.User, userComment);
        }
        while (!string.IsNullOrEmpty(userComment));

        return true;
    }

    private static bool IsCodeOnly(string result) => result.StartsWith(Prefix) && result.EndsWith(Postfix);
}

using Spectre.Console;

namespace MyAi.Tools;

public class WorkingDirectory
{
    public string GetWorkingDirectory()
    {
        var workingDirectory = Directory.GetCurrentDirectory();
        AnsiConsole.MarkupLine("[blue]Working directory:[/] {0}", workingDirectory.EscapeMarkup());
        return workingDirectory;
    }
}

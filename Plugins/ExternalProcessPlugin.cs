using System.ComponentModel;
using System.Diagnostics;
using Microsoft.SemanticKernel;
using Spectre.Console;

[Description("Plugin to interact with external processes.")]
internal sealed class ExternalProcessPlugin
{
    private readonly bool _debug;

    public ExternalProcessPlugin()
    {
        _debug = Env.Debug;
    }
    [KernelFunction("vscode_target_file")]
    [Description("Gets the file name currently open in the VSCode window.")]
    [return: Description("File name currently open in VSCode or an empty string if not found.")]
    public string VSCodeTargetFileName()
    {
        var processes = Process.GetProcessesByName("Code")
            .Where(x => !string.IsNullOrWhiteSpace(x.MainWindowTitle))
            .ToList();

        if (processes.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Not able to find any VSCode process.[/]");
            return string.Empty;
        }

        if (processes.Count > 1)
        {
            AnsiConsole.MarkupLine("[red]More than one VSCode process found.[/]");
            return string.Empty;
        }

        var process = processes.First();
        var splittedTitle = process.MainWindowTitle.Split('-');

        if (_debug)
        {
            AnsiConsole.MarkupLine("[green]VSCode process found with title:[/] {0}", process.MainWindowTitle.EscapeMarkup());
        }

        if (splittedTitle.Length >= 2)
        {
            string fileName = splittedTitle[0].Trim();
            if (_debug)
            {
                AnsiConsole.MarkupLine("[green]Extracted file name:[/] {0}", fileName.EscapeMarkup());
            }
            return fileName;
        }

        AnsiConsole.MarkupLine("[red]Not able to extract file name from VSCode process title.[/]");
        return string.Empty;
    }
}

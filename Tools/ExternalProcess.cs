using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Spectre.Console;

namespace MyAi.Tools;

public class ExternalProcess
{
    public string GetCurrentFileNameFromWindow(string processName)
    {
        AnsiConsole.MarkupLine($"[blue]Tracing: using processName: {processName}[/]");
        var mainWindowTitle = GetWindowTitleByName(processName);
        var splittedTitle = mainWindowTitle.Split('-');

        if (splittedTitle.Length >= 2)
        {
            string fileName = splittedTitle[0].Trim();
            AnsiConsole.MarkupLine("[green]Extracted file name:[/] {0}", fileName.EscapeMarkup());
            return fileName;
        }

        AnsiConsole.MarkupLine("[red]Not able to extract file name from [0] process title.[/]", processName);
        return string.Empty;
    }

    public string GetCurrentFileNameFromWindow(int processId)
    {
        AnsiConsole.MarkupLine($"[blue]Tracing: using processId: {processId}[/]");
        var mainWindowTitle = GetWindowTitleById(processId);
        var splittedTitle = mainWindowTitle.Split('-');

        if (splittedTitle.Length >= 2)
        {
            string fileName = splittedTitle[0].Trim();
            AnsiConsole.MarkupLine("[green]Extracted file name:[/] {0}", fileName.EscapeMarkup());
            return fileName;
        }

        AnsiConsole.MarkupLine("[red]Not able to extract file name from [0] process id.[/]", processId);
        return string.Empty;
    }

    public string GetWindowTitleByName(string processName)
    {
        var processes = Process.GetProcessesByName(processName)
            .Where(x => !string.IsNullOrWhiteSpace(x.MainWindowTitle))
            .ToList();

        if (processes.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Not able to find any process with the name:[/] {0}", processName.EscapeMarkup());
            return string.Empty;
        }

        if (processes.Count > 1)
        {
            AnsiConsole.MarkupLine("[red]More than one process found with the name:[/] {0}", processName.EscapeMarkup());
            return string.Empty;
        }

        var process = processes.First();
        string windowTitle = process.MainWindowTitle;

        AnsiConsole.MarkupLine("[green]Process found with title:[/] {0}", windowTitle.EscapeMarkup());

        return windowTitle;
    }

    public string GetWindowTitleById(int processId)
    {
        var process = Process.GetProcessById(processId);

        if (process is null)
        {
            return string.Empty;
        }

        string windowTitle = process.MainWindowTitle;

        return windowTitle;
    }

    public string GetFocusedWindowTitle()
    {
        IntPtr hWnd = GetForegroundWindow(); // Get the handle of the currently focused window
        if (hWnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("No window is currently focused.");
        }

        StringBuilder windowText = new(256);
        if (GetWindowText(hWnd, windowText, windowText.Capacity) > 0)
        {
            return windowText.ToString();
        }

        throw new InvalidOperationException("Unable to retrieve the window title.");
    }

    // WinAPI functions
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
}

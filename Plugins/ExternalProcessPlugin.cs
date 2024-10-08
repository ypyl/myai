﻿using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.SemanticKernel;
using Spectre.Console;

[Description("Plugin to interact with external processes.")]
internal sealed class ExternalProcessPlugin(string processName, int processId = 0)
{
    [KernelFunction("window_target_file_name")]
    [Description("Gets the name of file name.")]
    [return: Description("File name currently open or an empty string if not found.")]
    public string WindowTargetFileName()
    {
        var tracingString = processId > 0 ? $"using processId: {processId}" : $"using processName: {processName}";
        AnsiConsole.MarkupLine($"[blue]Tracing: {tracingString}[/]");
        var mainWindowTitle = processId > 0 ? GetWindowTitleById(processId) : GetWindowTitleByName(processName);
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
            AnsiConsole.MarkupLine("[red]Not able to find any process with the id:[/] {0}", processId);
            return string.Empty;
        }

        string windowTitle = process.MainWindowTitle;

        AnsiConsole.MarkupLine("[green]Process found with title:[/] {0}", windowTitle.EscapeMarkup());

        return windowTitle;
    }

    [KernelFunction("get_focused_window_title")]
    [Description("Gets the title of the currently focused window.")]
    [return: Description("Title of the currently focused window or an empty string if not found.")]
    public string GetFocusedWindowTitle()
    {
        IntPtr hWnd = GetForegroundWindow(); // Get the handle of the currently focused window
        if (hWnd == IntPtr.Zero)
        {
            AnsiConsole.MarkupLine("[red]No window is currently focused.[/]");
            return string.Empty;
        }

        StringBuilder windowText = new StringBuilder(256);
        if (GetWindowText(hWnd, windowText, windowText.Capacity) > 0)
        {
            string title = windowText.ToString();
            AnsiConsole.MarkupLine("[green]Focused window title:[/] {0}", title.EscapeMarkup());
            return title;
        }

        AnsiConsole.MarkupLine("[red]Unable to retrieve the title of the focused window.[/]");
        return string.Empty;
    }

    // WinAPI functions
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
}

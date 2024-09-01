using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.SemanticKernel;
using Spectre.Console;

[Description("Plugin to execute external applications.")]
internal sealed class ExternalAppPlugin
{
    private readonly bool _debug;
    private readonly string _workingDir;

    public ExternalAppPlugin()
    {
        _debug = Env.Debug;
        _workingDir = Env.WorkingDir;
    }
    [KernelFunction("execute_command")]
    [Description("Executes an external command and captures its output.")]
    [return: Description("The output of the executed command.")]
    public string ExecuteCommand(
        [Description("The name of the file to execute (e.g., git, dotnet).")] string fileName,
        [Description("The arguments to pass to the command.")] string arguments)
    {
        var outputBuilder = new StringBuilder();

        using Process process = new();
        var processStartInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            Arguments = arguments,
            FileName = fileName,
            WorkingDirectory = _workingDir,
        };

        process.StartInfo = processStartInfo;

        process.OutputDataReceived += new DataReceivedEventHandler(
            delegate (object sender, DataReceivedEventArgs e)
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                    if (_debug)
                    {
                        AnsiConsole.MarkupLine("[green]{0}[/]", e.Data.EscapeMarkup());
                    }
                }
            }
        );

        process.ErrorDataReceived += new DataReceivedEventHandler(
            delegate (object sender, DataReceivedEventArgs e)
            {
                if (e.Data != null)
                {
                    if (_debug)
                    {
                        AnsiConsole.MarkupLine("[red]{0}[/]", e.Data.EscapeMarkup());
                    }
                }
            }
        );

        AnsiConsole.Status().Start($"Executing {fileName}...", ctx =>
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        });

        return outputBuilder.ToString();
    }
}

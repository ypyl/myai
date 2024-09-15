using System.ComponentModel;
using Microsoft.SemanticKernel;
using Serilog;
using Spectre.Console;

[Description("Plugin to run dotnet commands.")]
internal sealed class DotNetPlugin(string workingDir, ILogger logger)
{
    [KernelFunction("dotnet_format")]
    [Description("Formats a single file using dotnet format.")]
    [return: Description("Dotnet format command output")]
    public string FormatFile([Description("Path to the file to format")] string filePath)
    {
        return AnsiConsole.Status().Start("Formatting file...", ctx =>
        {
            var csprojFile = new FileFinderPlugin(workingDir, logger).FindClosestCsprojFile();
            if (string.IsNullOrEmpty(csprojFile))
            {
                AnsiConsole.MarkupLine("[red]Error: No .csproj file found in the current or parent directories.[/]");
                return "[Error] No .csproj file found.";
            }
            var csprojFileName = Path.GetFileName(csprojFile);
            var relativePath = GetRelativePath(workingDir, filePath);
            return new ExternalAppPlugin(workingDir, logger).ExecuteCommand("dotnet", $"format {csprojFileName} --include {relativePath}");
        });
    }

    private static string GetRelativePath(string basePath, string filePath)
    {
        return System.IO.Path.GetRelativePath(basePath, filePath);
    }
}

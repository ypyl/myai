using System.ComponentModel;
using System.Text;
using Microsoft.SemanticKernel;
using Spectre.Console;

internal sealed class FileIOPlugin
{
    [KernelFunction, Description("Read a file")]
    public async Task<string> ReadAsync([Description("Source file")] string path)
    {
        AnsiConsole.MarkupLine("[green]Reading content of file:[/] [navy]{0}[/]", path);
        return await File.ReadAllTextAsync(path);
    }

    [KernelFunction, Description("Write a file")]
    public async Task WriteAsync(
        [Description("Destination file")] string path,
        [Description("File content")] string content)
    {
        var fileEndoding = GetEncoding(path);
        var fileEol = await DetectEOL(path);
        await File.WriteAllTextAsync(path, content.TrimStart().Replace("\r\n", fileEol).Replace("\n", fileEol), fileEndoding);
    }

    public static Encoding GetEncoding(string filename)
    {
        using var reader = new StreamReader(filename, Encoding.UTF8, true);
        reader.Peek();
        return reader.CurrentEncoding;
    }

    public async Task<string> DetectEOL(string filePath)
    {
        string content = await File.ReadAllTextAsync(filePath);

        // Check for Windows EOL first (\r\n)
        if (content.Contains("\r\n"))
        {
            return "\r\n";
        }
        // Check for Unix/Linux EOL (\n)
        else if (content.Contains("\n"))
        {
            return "\n";
        }

        return Environment.NewLine;
    }
}

using System.ComponentModel;
using System.Text;
using Microsoft.SemanticKernel;
using Spectre.Console;

internal sealed class FileIOPlugin
{
    [KernelFunction, Description("Read a file")]
    public async Task<string> ReadAsync([Description("Source file")] string path)
    {
        AnsiConsole.MarkupLine("[green]Reading content of file:[/] {0}", path);
        return await File.ReadAllTextAsync(path);
    }

    [KernelFunction, Description("Write a file")]
    public async Task WriteAsync(
        [Description("Destination file")] string path,
        [Description("File content")] string content)
    {
        var fileEndoding = GetEncoding(path);
        await File.WriteAllTextAsync(path, content.TrimStart(), fileEndoding);
    }

    public static Encoding GetEncoding(string filename)
    {
        using var reader = new StreamReader(filename, Encoding.UTF8, true);
        reader.Peek(); // you need this!
        return reader.CurrentEncoding;
    }
}

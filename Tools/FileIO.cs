using System.ComponentModel;
using System.Text;
using Spectre.Console;

public sealed class FileIO
{
    public async Task<string> ReadAsync(string path)
    {
        return await File.ReadAllTextAsync(path);
    }

    public async Task WriteAsync(string path, string content)
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

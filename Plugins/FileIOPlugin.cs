using System.ComponentModel;
using Microsoft.SemanticKernel;

internal sealed class FileIOPlugin
{
    [KernelFunction, Description("Read a file")]
    public async Task<string> ReadAsync([Description("Source file")] string path)
    {
        return await File.ReadAllTextAsync(path);
    }

    [KernelFunction, Description("Write a file")]
    public async Task WriteAsync(
        [Description("Destination file")] string path,
        [Description("File content")] string content)
    {
        await File.WriteAllTextAsync(path, content);
    }
}

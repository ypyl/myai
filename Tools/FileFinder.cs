using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace MyAi.Tools;

public sealed class FileFinder(ILogger<FileFinder> logger)
{
    public Dictionary<string, string> FindCsFiles(string dir) => FindFiles(dir, "*.cs", ["obj", "bin"], ".Design.cs");

    public Dictionary<string, string> FindTsFiles(string dir)
    {
        var tsFiles = FindFiles(dir, "*.ts", ["node_modules"]);
        var tsxFiles = FindFiles(dir, "*.tsx", ["node_modules"]);
        foreach (var file in tsxFiles)
        {
            tsFiles[file.Key] = file.Value;
        }
        return tsFiles;
    }

    public Dictionary<string, string> FindCsprojFiles(string dir) => FindFiles(dir, "*.csproj");

    public Dictionary<string, string> FindJsonFiles(string dir) => FindFiles(dir, "*.json");

    public string? FindClosestCsprojFile(string workingDir)
    {
        string currentDir = workingDir;
        while (!Directory.GetFiles(currentDir, "*.csproj").Any())
        {
            var parentDir = Path.GetDirectoryName(currentDir);
            if (string.IsNullOrEmpty(parentDir) || parentDir == currentDir)
            {
                return null;
            }
            currentDir = parentDir;
        }
        return Directory.GetFiles(currentDir, "*.csproj").First();
    }

    private Dictionary<string, string> FindFiles(string dir, string searchPattern, string[]? excludeDirs = null, string? excludeSuffix = null)
    {
        if (string.IsNullOrWhiteSpace(dir))
        {
            AnsiConsole.MarkupLine("[red]Error: Folder path cannot be null or empty.[/]");
            return new Dictionary<string, string>();
        }

        if (!Directory.Exists(dir))
        {
            AnsiConsole.MarkupLine("[red]Error: The specified folder path does not exist: {0}[/]", dir.EscapeMarkup());
            return new Dictionary<string, string>();
        }

        return AnsiConsole.Status().Start($"Searching for {searchPattern} files...", ctx =>
        {
            var filesDictionary = new Dictionary<string, string>();
            var files = Directory.GetFiles(dir, searchPattern, SearchOption.AllDirectories)
                .Where(file => (excludeDirs == null || !excludeDirs.Any(excludeDir => file.Contains(Path.DirectorySeparatorChar + excludeDir + Path.DirectorySeparatorChar))) &&
                               (excludeSuffix == null || !file.EndsWith(excludeSuffix, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (files.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No {searchPattern} files found in the specified folder.[/]");
            }

            foreach (var file in files)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                filesDictionary[fileNameWithoutExtension] = file;

                logger.LogTrace("Found: {fileNameWithoutExtension} -> {file}", fileNameWithoutExtension, file);
            }

            return filesDictionary;
        });
    }
}

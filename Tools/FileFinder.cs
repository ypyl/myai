using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace MyAi.Tools;

public sealed class FileFinder(ILogger<FileFinder> logger)
{
    public Dictionary<string, string> FindCsFiles(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir))
        {
            AnsiConsole.MarkupLine("[red]Error: Folder path cannot be null or empty.[/]");
            return [];
        }

        if (!Directory.Exists(dir))
        {
            AnsiConsole.MarkupLine("[red]Error: The specified folder path does not exist: {0}[/]", dir.EscapeMarkup());
            return [];
        }

        return AnsiConsole.Status().Start("Searching for .cs files...", ctx =>
        {
            var csFilesDictionary = new Dictionary<string, string>();

            var csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                               !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                               !file.EndsWith(".Design.cs", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (csFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No .cs files found in the specified folder.[/]");
            }

            foreach (var file in csFiles)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                csFilesDictionary[fileNameWithoutExtension] = file;

                logger.LogTrace("Found: {fileNameWithoutExtension} -> {file}", fileNameWithoutExtension, file);
            }

            return csFilesDictionary;
        });
    }

    public Dictionary<string, string> FindTsFiles(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir))
        {
            AnsiConsole.MarkupLine("[red]Error: Folder path cannot be null or empty.[/]");
            return [];
        }

        if (!Directory.Exists(dir))
        {
            AnsiConsole.MarkupLine("[red]Error: The specified folder path does not exist: {0}[/]", dir.EscapeMarkup());
            return [];
        }

        return AnsiConsole.Status().Start("Searching for .ts files...", ctx =>
        {
            var tsFilesDictionary = new Dictionary<string, string>();
            var tsFiles = Directory.GetFiles(dir, "*.ts", SearchOption.AllDirectories)
                .Where(file => !file.Contains(Path.DirectorySeparatorChar + "node_modules" + Path.DirectorySeparatorChar))
                .ToList();
            var tsxFiles = Directory.GetFiles(dir, "*.tsx", SearchOption.AllDirectories)
                .Where(file => !file.Contains(Path.DirectorySeparatorChar + "node_modules" + Path.DirectorySeparatorChar))
                .ToList();
            tsFiles.AddRange(tsxFiles);

            if (tsFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No .ts files found in the specified folder.[/]");
            }

            foreach (var file in tsFiles)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                tsFilesDictionary[fileNameWithoutExtension] = file;

                logger.LogTrace("Found: {fileNameWithoutExtension} -> {file}", fileNameWithoutExtension, file);
            }

            return tsFilesDictionary;
        });
    }

    public Dictionary<string, string> FindCsprojFiles(string dir)
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

        return AnsiConsole.Status().Start("Searching for .csproj files...", ctx =>
        {
            var csprojFilesDictionary = new Dictionary<string, string>();

            var csprojFiles = Directory.GetFiles(dir, "*.csproj", SearchOption.AllDirectories)
                .ToList();

            if (csprojFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No .csproj files found in the specified folder.[/]");
            }

            foreach (var file in csprojFiles)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                csprojFilesDictionary[fileNameWithoutExtension] = file;

                logger.LogTrace("Found: {fileNameWithoutExtension} -> {file}", fileNameWithoutExtension, file);
            }

            return csprojFilesDictionary;
        });
    }

    public Dictionary<string, string> FindJsonFiles(string workingDir)
    {
        if (string.IsNullOrWhiteSpace(workingDir))
        {
            AnsiConsole.MarkupLine("[red]Error: Folder path cannot be null or empty.[/]");
            return new Dictionary<string, string>();
        }

        if (!Directory.Exists(workingDir))
        {
            AnsiConsole.MarkupLine("[red]Error: The specified folder path does not exist: {0}[/]", workingDir.EscapeMarkup());
            return new Dictionary<string, string>();
        }

        return AnsiConsole.Status().Start("Searching for .json files...", ctx =>
        {
            var jsonFilesDictionary = new Dictionary<string, string>();

            var jsonFiles = Directory.GetFiles(workingDir, "*.json", SearchOption.AllDirectories)
                .ToList();

            if (jsonFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No .json files found in the specified folder.[/]");
            }

            foreach (var file in jsonFiles)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                jsonFilesDictionary[fileNameWithoutExtension] = file;

                logger.LogTrace("Found: {fileNameWithoutExtension} -> {file}", fileNameWithoutExtension, file);
            }

            return jsonFilesDictionary;
        });
    }

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
}

using System.ComponentModel;
using Microsoft.SemanticKernel;
using Serilog;
using Spectre.Console;

[Description("Plugin to find all C# files in a directory.")]
internal sealed class FileFinderPlugin(string workingDir, ILogger logger)
{
    [KernelFunction("find_cs_files")]
    [Description("Finds all .cs files in the specified folder and returns a dictionary with file names (without extension) as keys and full paths as values, ignoring files in 'obj' and 'bin' folders, and those ending with '.Design.cs'.")]
    [return: Description("Dictionary with file names as keys and full paths as values")]
    public Dictionary<string, string> FindCsFiles()
    {
        if (string.IsNullOrWhiteSpace(workingDir))
        {
            AnsiConsole.MarkupLine("[red]Error: Folder path cannot be null or empty.[/]");
            return [];
        }

        if (!Directory.Exists(workingDir))
        {
            AnsiConsole.MarkupLine("[red]Error: The specified folder path does not exist: {0}[/]", workingDir.EscapeMarkup());
            return [];
        }

        return AnsiConsole.Status().Start("Searching for .cs files...", ctx =>
        {
            var csFilesDictionary = new Dictionary<string, string>();

            var csFiles = Directory.GetFiles(workingDir, "*.cs", SearchOption.AllDirectories)
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

                logger.Verbose(string.Format("Found: {0} -> {1}", fileNameWithoutExtension, file));
            }

            return csFilesDictionary;
        });
    }

    [KernelFunction("find_csproj_files")]
    [Description("Finds all .csproj files in the specified folder and returns a dictionary with file names (without extension) as keys and full paths as values.")]
    [return: Description("Dictionary with file names as keys and full paths as values")]
    public Dictionary<string, string> FindCsprojFiles()
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

        return AnsiConsole.Status().Start("Searching for .csproj files...", ctx =>
        {
            var csprojFilesDictionary = new Dictionary<string, string>();

            var csprojFiles = Directory.GetFiles(workingDir, "*.csproj", SearchOption.AllDirectories)
                .ToList();

            if (csprojFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No .csproj files found in the specified folder.[/]");
            }

            foreach (var file in csprojFiles)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                csprojFilesDictionary[fileNameWithoutExtension] = file;

                logger.Verbose(string.Format("Found: {0} -> {1}", fileNameWithoutExtension, file));
            }

            return csprojFilesDictionary;
        });
    }

    [KernelFunction("find_json_files")]
    [Description("Finds all .json files in the specified folder and returns a dictionary with file names (without extension) as keys and full paths as values.")]
    [return: Description("Dictionary with file names as keys and full paths as values")]
    public Dictionary<string, string> FindJsonFiles()
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

                logger.Verbose(string.Format("Found: {0} -> {1}", fileNameWithoutExtension, file));
            }

            return jsonFilesDictionary;
        });
    }

    [KernelFunction("find_closest_csproj_file")]
    [Description("Finds the closest .csproj file to the current directory.")]
    [return: Description("Path to the closest .csproj file or null if not found")]
    public string FindClosestCsprojFile()
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

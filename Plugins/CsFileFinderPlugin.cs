using System.ComponentModel;
using Microsoft.SemanticKernel;
using Spectre.Console;

[Description("Plugin to find all C# files in a directory.")]
internal sealed class CsFileFinderPlugin
{
    private readonly bool _debug;
    private readonly string _workingDir;

    public CsFileFinderPlugin()
    {
        _debug = Env.Debug;
        _workingDir = Env.WorkingDir;
    }

    [KernelFunction("find_cs_files")]
    [Description("Finds all .cs files in the specified folder and returns a dictionary with file names (without extension) as keys and full paths as values, ignoring files in 'obj' and 'bin' folders, and those ending with '.Design.cs'.")]
    [return: Description("Dictionary with file names as keys and full paths as values")]
    public Dictionary<string, string> FindCsFiles()
    {
        // Validate the input path
        if (string.IsNullOrWhiteSpace(_workingDir))
        {
            AnsiConsole.MarkupLine("[red]Error: Folder path cannot be null or empty.[/]");
            return [];
        }

        if (!Directory.Exists(_workingDir))
        {
            AnsiConsole.MarkupLine("[red]Error: The specified folder path does not exist: {0}[/]", _workingDir.EscapeMarkup());
            return [];
        }

        return AnsiConsole.Status().Start("Searching for .cs files...", ctx =>
        {
            // Dictionary to store the file names and their paths
            var csFilesDictionary = new Dictionary<string, string>();

            // Get all .cs files in the specified folder (including subdirectories)
            var csFiles = Directory.GetFiles(_workingDir, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && // Exclude obj folder
                               !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) && // Exclude bin folder
                               !file.EndsWith(".Design.cs", StringComparison.OrdinalIgnoreCase)) // Exclude files ending with .Design.cs
                .ToList();

            if (csFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No .cs files found in the specified folder.[/]");
            }

            // Populate the dictionary with file names (without extension) as keys and full paths as values
            foreach (var file in csFiles)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                csFilesDictionary[fileNameWithoutExtension] = file;

                if (_debug)
                {
                    AnsiConsole.MarkupLine("[green]Found:[/] {0} -> [blue]{1}[/]", fileNameWithoutExtension.EscapeMarkup(), file.EscapeMarkup());
                }
            }

            return csFilesDictionary;
        });
    }
}

using System.Text.Json;
using Spectre.Console;
using Newtonsoft.Json.Linq;

internal class Config
{
    private JObject? _jsonObject;

    public Config(string filePath = "settings.json")
    {
        Load(filePath);
    }

    public JToken? GetValue(string jsonPath)
    {
        return _jsonObject?.SelectToken(jsonPath);
    }

    public string GetStringValue(string jsonPath)
    {
        // Use SelectToken to query the JSON and get the result as a string
        return _jsonObject?.SelectToken(jsonPath)?.Value<string>() ?? string.Empty;
    }

    public bool GetBoolValue(string jsonPath)
    {
        // Use SelectToken to query the JSON and get the result as a string
        return _jsonObject?.SelectToken(jsonPath)?.Value<bool>() ?? false;
    }

    private void Load(string filePath)
    {
        // Check if the path is a full path or just a file name
        var fullPath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(Directory.GetCurrentDirectory(), filePath);

        // Try to find the file (search up the directory structure if necessary)
        fullPath = FindFileUpInHierarchy(fullPath);

        if (fullPath is null)
        {
            AnsiConsole.WriteLine($"[yellow]Warning: The file '{filePath}' does not exist. Skipping...[/]");
            return; // Exit if file not found
        }

        try
        {
            // Read the JSON content from the file
            string jsonContent = File.ReadAllText(fullPath);

            // Parse the JSON content into a JsonDocument
            _jsonObject = JObject.Parse(jsonContent);
        }
        catch (JsonException ex)
        {
            AnsiConsole.WriteLine($"[red]Error: Invalid JSON format in file '{fullPath}'.[/]");
            AnsiConsole.WriteException(ex);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine($"[red]Error: Could not load file '{fullPath}'.[/]");
            AnsiConsole.WriteException(ex);
        }
    }

    // Helper function to find a file by searching up the directory structure
    private static string? FindFileUpInHierarchy(string path)
    {
        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileName(path);

        while (!string.IsNullOrEmpty(directory))
        {
            string potentialFilePath = Path.Combine(directory, fileName);
            if (File.Exists(potentialFilePath))
            {
                return potentialFilePath;
            }

            // Move to the parent directory
            directory = Directory.GetParent(directory)?.FullName;
        }

        return null; // File not found
    }
}

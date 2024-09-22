using System.Text.Json;
using Spectre.Console;
using Newtonsoft.Json.Linq;

internal class Config
{
    private readonly List<JObject> _jsonObjects = [];

    public Config(string filePath = "settings.json")
    {
        Load(filePath);
    }

    private JToken? GetValue(string jsonPath)
    {
        foreach (var jsonObject in _jsonObjects)
        {
            var result = jsonObject.SelectToken(jsonPath);
            if (result is not null) return result;
        }
        return null;
    }

    public string GetStringValue(string jsonPath)
    {
        return GetValue(jsonPath)?.Value<string>() ?? string.Empty;
    }

    public int GetIntValue(string jsonPath)
    {
        return GetValue(jsonPath)?.Value<int>() ?? default;
    }

    public bool GetBoolValue(string jsonPath)
    {
        return GetValue(jsonPath)?.Value<bool>() ?? default;
    }

    private void Load(string filePath)
    {
        // Check if the path is a full path or just a file name
        var fullPath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(Directory.GetCurrentDirectory(), filePath);

        // Try to find the file (search up the directory structure if necessary)
        var settingFiles = FindFileUpInHierarchy(fullPath);

        if (settingFiles.Count == 0)
        {
            AnsiConsole.WriteLine($"[yellow]Warning: The file '{filePath}' does not exist. Skipping...[/]");
            return; // Exit if file not found
        }

        try
        {
            foreach (var settingFile in settingFiles)
            {
                // Read the JSON content from the file
                string jsonContent = File.ReadAllText(settingFile);

                // Parse the JSON content into a JsonDocument
                _jsonObjects.Add(JObject.Parse(jsonContent));
            }
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
    private static List<string> FindFileUpInHierarchy(string path)
    {
        var settingsFile = new List<string>();
        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileName(path);

        while (!string.IsNullOrEmpty(directory))
        {
            string potentialFilePath = Path.Combine(directory, fileName);
            if (File.Exists(potentialFilePath))
            {
                settingsFile.Add(potentialFilePath);
            }

            // Move to the parent directory
            directory = Directory.GetParent(directory)?.FullName;
        }

        return settingsFile;
    }
}

using Microsoft.Extensions.Logging;

namespace MyAi.Tools;

public sealed class FileFinder
{
    private readonly ILogger<FileFinder> _logger;

    public FileFinder(ILogger<FileFinder> logger)
    {
        _logger = logger;
    }

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
            throw new ArgumentException("Folder path cannot be null or empty.", nameof(dir));
        }

        if (!Directory.Exists(dir))
        {
            throw new DirectoryNotFoundException($"The specified folder path does not exist: {dir}");
        }

        var filesDictionary = new Dictionary<string, string>();
        var files = Directory.GetFiles(dir, searchPattern, SearchOption.AllDirectories)
            .Where(file => (excludeDirs == null || !excludeDirs.Any(excludeDir => file.Contains(Path.DirectorySeparatorChar + excludeDir + Path.DirectorySeparatorChar))) &&
                            (excludeSuffix == null || !file.EndsWith(excludeSuffix, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var file in files)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            filesDictionary[fileNameWithoutExtension] = file;

            _logger.LogTrace("Found: {fileNameWithoutExtension} -> {file}", fileNameWithoutExtension, file);
        }

        return filesDictionary;
    }
}

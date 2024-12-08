using System.Text;
using Microsoft.Extensions.Logging;

namespace MyAi.Tools;

public class DirectoryPacker
{
    private readonly ILogger<DirectoryPacker> _logger;

    public DirectoryPacker(ILogger<DirectoryPacker> logger)
    {
        _logger = logger;
    }

    public string Pack(string path)
    {
        _logger.LogInformation("Starting Pack method for path: {Path}", path);
        var supportedExtensions = new[] { ".cs", ".ts", ".tsx" };
        var directoryStructure = new StringBuilder();
        var directoryFiles = new StringBuilder();

        directoryStructure.Append("================================================================\n");
        directoryStructure.Append("Directory Structure\n");
        directoryStructure.Append("================================================================\n");

        var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                             .Where(file => supportedExtensions.Contains(Path.GetExtension(file)))
                             .ToList();

        _logger.LogInformation("Found {FileCount} files with supported extensions", files.Count);

        var directories = files.Select(Path.GetDirectoryName)
                               .Distinct()
                               .OrderBy(dir => dir);

        foreach (var dir in directories)
        {
            if (dir is null) continue;
            var relativeDir = Path.GetRelativePath(path, dir).Replace("\\", "/");
            directoryStructure.Append(relativeDir + "/\n");

            var dirFiles = files.Where(file => Path.GetDirectoryName(file) == dir)
                                .Select(file => Path.GetFileName(file))
                                .OrderBy(file => file);

            foreach (var file in dirFiles)
            {
                directoryStructure.Append("  " + file + "\n");
            }
        }

        directoryFiles.Append("================================================================\n");
        directoryFiles.Append("Directory Files\n");
        directoryFiles.Append("================================================================\n");

        foreach (var file in files)
        {
            var relativeFile = Path.GetRelativePath(path, file).Replace("\\", "/");
            directoryFiles.Append("================\n");
            directoryFiles.Append($"File: {relativeFile}\n");
            directoryFiles.Append("================\n");
            directoryFiles.Append(File.ReadAllText(file) + "\n\n");
        }

        return directoryStructure.ToString() + directoryFiles.ToString();
    }

    public string PackFiles(string[] filePaths)
    {
        if (filePaths.Length == 0)
        {
            _logger.LogWarning("No file paths provided");
            return string.Empty;
        }
        _logger.LogInformation("Starting PackFiles method for file paths: {FilePaths}", string.Join(", ", filePaths));
        var repositoryFiles = new StringBuilder();

        foreach (var file in filePaths)
        {
            if (File.Exists(file))
            {
                var relativeFile = Path.GetFileName(file).Replace("\\", "/");
                repositoryFiles.Append("================\n");
                repositoryFiles.Append($"File: {relativeFile}\n");
                repositoryFiles.Append("================\n");
                repositoryFiles.Append(File.ReadAllText(file) + "\n\n");
            }
            else
            {
                _logger.LogWarning("File not found: {FilePath}", file);
            }
        }

        return repositoryFiles.ToString();
    }

    public string PackFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            _logger.LogWarning("No file path provided");
            return string.Empty;
        }

        _logger.LogInformation("Starting PackFile method for file path: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            return string.Empty;
        }

        var repositoryFile = new StringBuilder();

        var relativeFile = Path.GetFileName(filePath).Replace("\\", "/");
        repositoryFile.Append("================\n");
        repositoryFile.Append($"File: {relativeFile}\n");
        repositoryFile.Append("================\n");
        repositoryFile.Append(File.ReadAllText(filePath) + "\n\n");

        return repositoryFile.ToString();
    }

    public string GetFileContent(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            _logger.LogWarning("No file path provided");
            return string.Empty;
        }

        _logger.LogInformation("Starting GetFileContent method for file path: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            return string.Empty;
        }

        return File.ReadAllText(filePath);
    }
}

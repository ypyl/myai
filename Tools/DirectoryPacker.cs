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
        var repositoryStructure = new StringBuilder();
        var repositoryFiles = new StringBuilder();

        repositoryStructure.AppendLine("================================================================");
        repositoryStructure.AppendLine("Repository Structure");
        repositoryStructure.AppendLine("================================================================");

        var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                             .Where(file => supportedExtensions.Contains(Path.GetExtension(file)))
                             .ToList();

        _logger.LogInformation("Found {FileCount} files with supported extensions", files.Count);

        var directories = files.Select(Path.GetDirectoryName)
                               .Distinct()
                               .OrderBy(dir => dir);

        foreach (var dir in directories)
        {
            var relativeDir = Path.GetRelativePath(path, dir).Replace("\\", "/");
            repositoryStructure.AppendLine(relativeDir + "/");

            var dirFiles = files.Where(file => Path.GetDirectoryName(file) == dir)
                                .Select(file => Path.GetFileName(file))
                                .OrderBy(file => file);

            foreach (var file in dirFiles)
            {
                repositoryStructure.AppendLine("  " + file);
            }
        }

        repositoryFiles.AppendLine("================================================================");
        repositoryFiles.AppendLine("Repository Files");
        repositoryFiles.AppendLine("================================================================");

        foreach (var file in files)
        {
            var relativeFile = Path.GetRelativePath(path, file).Replace("\\", "/");
            repositoryFiles.AppendLine("================");
            repositoryFiles.AppendLine($"File: {relativeFile}");
            repositoryFiles.AppendLine("================");
            repositoryFiles.AppendLine(File.ReadAllText(file));
            repositoryFiles.AppendLine();
        }

        return repositoryStructure.ToString() + repositoryFiles.ToString();
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

        repositoryFiles.AppendLine("================================================================");
        repositoryFiles.AppendLine("Repository Files");
        repositoryFiles.AppendLine("================================================================");

        foreach (var file in filePaths)
        {
            if (File.Exists(file))
            {
                var relativeFile = Path.GetFileName(file).Replace("\\", "/");
                repositoryFiles.AppendLine("================");
                repositoryFiles.AppendLine($"File: {relativeFile}");
                repositoryFiles.AppendLine("================");
                repositoryFiles.AppendLine(File.ReadAllText(file));
                repositoryFiles.AppendLine();
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
        repositoryFile.AppendLine("================");
        repositoryFile.AppendLine($"File: {relativeFile}");
        repositoryFile.AppendLine("================");
        repositoryFile.AppendLine(File.ReadAllText(filePath));
        repositoryFile.AppendLine();

        return repositoryFile.ToString();
    }
}

using System.Text;
using Microsoft.Extensions.Logging;

namespace MyAi.Tools;

public class DirectoryPacker(ILogger<DirectoryPacker> logger)
{
    public string Pack(string path)
    {
        logger.LogInformation("Starting Pack method for path: {Path}", path);
        var supportedExtensions = new[] { ".cs", ".ts", ".tsx" };
        var repositoryStructure = new StringBuilder();
        var repositoryFiles = new StringBuilder();

        repositoryStructure.AppendLine("================================================================");
        repositoryStructure.AppendLine("Repository Structure");
        repositoryStructure.AppendLine("================================================================");

        var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                             .Where(file => supportedExtensions.Contains(Path.GetExtension(file)))
                             .ToList();

        logger.LogInformation("Found {FileCount} files with supported extensions", files.Count);

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
        logger.LogInformation("Starting PackFiles method for file paths: {FilePaths}", string.Join(", ", filePaths));
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
                logger.LogWarning("File not found: {FilePath}", file);
            }
        }

        return repositoryFiles.ToString();
    }
}

using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.SemanticKernel;
using Serilog;

[Description("Plugin to execute external applications.")]
internal sealed class ExternalAppPlugin(string workingDir, ILogger logger)
{
    [KernelFunction("execute_command")]
    [Description("Executes an external command and captures its output.")]
    [return: Description("The output of the executed command.")]
    public string ExecuteCommand(
        [Description("The name of the file to execute (e.g., git, dotnet).")] string fileName,
        [Description("The arguments to pass to the command.")] string arguments)
    {
        var outputBuilder = new StringBuilder();

        using Process process = new();
        var processStartInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            Arguments = arguments,
            FileName = fileName,
            WorkingDirectory = workingDir,
        };

        process.StartInfo = processStartInfo;

        process.OutputDataReceived += new DataReceivedEventHandler(
            delegate (object sender, DataReceivedEventArgs e)
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                    logger.Verbose(e.Data);
                }
            }
        );

        process.ErrorDataReceived += new DataReceivedEventHandler(
            delegate (object sender, DataReceivedEventArgs e)
            {
                if (e.Data != null)
                {
                    logger.Error(e.Data);
                }
            }
        );

        logger.Verbose(nameof(ExecuteCommand));
        logger.Verbose(fileName);
        logger.Verbose(arguments);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return outputBuilder.ToString();
    }
}

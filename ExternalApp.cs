
using System.Diagnostics;
using System.Text;

internal sealed class ExternalApp
{
    public static string Execute(string fileName, string arguments, Action<string?> processFunc, string workingDirectory, Action<string?> processErrorFunc)
    {
        using Process process = new();

        var outputBuilder = new StringBuilder();

        var processStartInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            Arguments = arguments,
            FileName = fileName,
            WorkingDirectory = workingDirectory,
        };

        process.StartInfo = processStartInfo;

        process.OutputDataReceived += new DataReceivedEventHandler
        (
            delegate (object sender, DataReceivedEventArgs e)
            {
                processFunc(e.Data);
                outputBuilder.AppendLine(e.Data);
            }
        );
        process.ErrorDataReceived += new DataReceivedEventHandler
        (
            delegate (object sender, DataReceivedEventArgs e)
            {
                processErrorFunc(e.Data);
            }
        );
        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
        process.CancelOutputRead();

        return outputBuilder.ToString();
    }
}

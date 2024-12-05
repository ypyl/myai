namespace MyAi.Tools;
public class VSCode
{
    private string[] SplitWindowTitle(string windowTitle)
    {
        if (string.IsNullOrEmpty(windowTitle))
        {
            throw new ArgumentException("Window title cannot be null or empty", nameof(windowTitle));
        }

        return windowTitle.Split(new[] { " - " }, StringSplitOptions.None);
    }

    public string ParseWindowTitle(string windowTitle)
    {
        var parts = SplitWindowTitle(windowTitle);
        if (parts.Length > 0)
        {
            return parts[0];
        }

        throw new FormatException("Window title format is incorrect");
    }

    public bool IsValidVSCodeWindowTitle(string windowTitle)
    {
        var parts = SplitWindowTitle(windowTitle);
        return parts.Length == 3 && parts[2] == "Visual Studio Code";
    }
}

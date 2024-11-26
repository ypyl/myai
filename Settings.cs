public class Settings
{
    public bool Debug { get; set; }
    public int Pid { get; set; }
    public string? ProcessName { get; set; }
    public string? RemoteRepositoryName { get; set; }
    public string? WorkingDir { get; set; }
    public string? LogDir { get; set; }
    public string? System { get; set; }
    public ModelSettings? Model { get; set; }
    public CodeSettings? Code { get; set; }
}

public class ModelSettings
{
    public string? ModelId { get; set; }
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
}

public class CodeSettings
{
    public CSharpSettings? CSharp { get; set; }
}

public class CSharpSettings
{
    public string? WorkingDir { get; set; }
    public string? System { get; set; }
    public string? UserMessageCode { get; set; }
    public string? AdditionalContext { get; set; }
    public string? TypesFromInstructions { get; set; }
}

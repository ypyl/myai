using dotenv.net;

internal static class Env
{
    private static readonly IDictionary<string, string> _envVars;

    static Env()
    {
        DotEnv.Load();
        _envVars = DotEnv.Read();
    }

    public static bool Debug => _envVars["DEBUG"] == "true";
    public static string AIProvider => _envVars["AI_PROVIDER"];
    public static string WorkingDir => _envVars["WORKING_DIR"];
    public static string GroqModelId => _envVars["GROQ_MODEL_ID"];
    public static string GorqEndpoint => _envVars["GROQ_ENDPOINT"];
    public static string GroqApiKey => _envVars["GROQ_API_KEY"];
    public static string AzureOpenAIDeployment => _envVars["AZURE_OPENAI_DEPLOYMENT"];
    public static string AzureOpenAIEndpoint => _envVars["AZURE_OPENAI_ENDPOINT"];
    public static string AzureOpenAIApiKey => _envVars["AZURE_OPENAI_API_KEY"];

    public static class SystemPrompts
    {
        public static string SeniorSoftwareDeveloper => _envVars["SYSTEM_PROMPT_SENIOR_SOFTWARE_DEVELOPER"];
    }

    public static class UserPrompts
    {
        public static class GitCommit
        {
            public static string Main => _envVars["USER_PROMPT_GIT_COMMIT_MAIN"];
            public static string Regenerate => _envVars["USER_PROMPT_GIT_COMMIT_REGENERATE"];
        }
        public static class Code
        {
            public static string Main => _envVars["USER_PROMPT_CODE_MAIN"];
            public static string TypesFromInstructions => _envVars["USER_PROMPT_CODE_TYPES_FROM_INSTRUCTION"];
            public static string Regenerate => _envVars["USER_PROMPT_CODE_REGENERATE"];
        }
    }
}

﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using Spectre.Console;
using Spectre.Console.Cli;
using MyAi.Tools;
using MyAi.Code;
using MyAi;
using Azure.AI.OpenAI;
using OpenAI;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var serviceCollection = new ServiceCollection();

serviceCollection.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Trace));
serviceCollection.AddScoped<PromptBuilder>();
serviceCollection.AddScoped<GenerateCode>();
serviceCollection.AddTransient<Conversation>();
serviceCollection.AddScoped<ExternalProcess>();
serviceCollection.AddScoped<ExternalContext>();
serviceCollection.AddScoped<FileFinder>();
serviceCollection.AddScoped<GenerateCode>();
serviceCollection.AddScoped<CsNonStandardTypeExtractorPlugin>();
serviceCollection.AddScoped<ExternalTypesFromInstructionContext>();
serviceCollection.AddScoped<TsNonStandardModuleExtractorPlugin>();
serviceCollection.AddScoped<FileIO>();
serviceCollection.AddScoped<AutoFixLlmAnswer>();
serviceCollection.AddScoped<WorkingDirectory>();
serviceCollection.AddScoped<IConfiguration>(provider => configuration);
serviceCollection.Configure<AddLoggingOptions>(configuration);

var openAILink = Environment.GetEnvironmentVariable("MYAI_URI");
var openAIKey = Environment.GetEnvironmentVariable("MYAI_KEY");
var openAIModel = Environment.GetEnvironmentVariable("MYAI_MODEL");

var chatClient = openAILink is not null && openAIKey is not null && openAIModel is not null
    ? new AzureOpenAIClient(new Uri(openAILink), new System.ClientModel.ApiKeyCredential(openAIKey)).AsChatClient(openAIModel)
    : new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.2");

serviceCollection.AddChatClient(services =>
    new ChatClientBuilder(chatClient)
        .UseLogging(services.GetRequiredService<ILoggerFactory>())
        .UseFunctionInvocation()
        .Build());

var registrar = new TypeRegistrar(serviceCollection);

serviceCollection.AddSingleton<IConfiguration>(configuration);

var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.AddCommand<CodeCommand>("code")
        .WithDescription("Complete code in opened VSCode file")
        .WithExample("code");
    // config.AddCommand<GitCommitCommand>("commit")
    //     .WithDescription("Commit staged changes using generated message")
    //     .WithExample("commit");
    // config.AddCommand<GitDiffCommand>("diff")
    //     .WithDescription("Sum up diff between current branch and target")
    //     .WithExample("diff [targetBranch]");

    // config.AddCommand<SnippetCommand>("snippet")
    //     .WithDescription("Implement code snippet in opened VSCode file")
    //     .WithExample("snippet");
    // config.AddCommand<JsonCommand>("json")
    //     .WithDescription("Complete json in opened VSCode file")
    //     .WithExample("json");
    // config.AddCommand<ExplainCommand>("explain")
    //     .WithDescription("Explain code by adding comments to it in opened VSCode file")
    //     .WithExample("explain");
    // config.AddCommand<AddLoggingCommand>("logging")
    //     .WithDescription("Add logging to code by using ILogger to it in opened VSCode file")
    //     .WithExample("logging");
    // config.AddCommand<CoFCommand>("cof")
    //     .WithDescription("Chain of thought")
    //     .WithExample("cof");

    config.SetExceptionHandler((ex, typeResolver) =>
    {
        AnsiConsole.WriteException(ex, ExceptionFormats.Default);
    });
});
return app.Run(args);

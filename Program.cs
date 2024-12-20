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
using Stubble.Core.Builders;
using Stubble.Core.Interfaces;
using System.ClientModel;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddYamlFile("appsettings.code.csharp.yml", true)
    .AddYamlFile("appsettings.model.yml", true)
    // TODO add environment variables
    .Build();

var serviceCollection = new ServiceCollection();

serviceCollection.AddLogging(configure => configure.AddConfiguration(configuration.GetSection("Logging")).AddConsole());
serviceCollection.AddScoped<IStubbleRenderer>((provider) => new StubbleBuilder().Build());
serviceCollection.AddScoped<PromptBuilder>();
serviceCollection.AddTransient<Conversation>();

serviceCollection.AddScoped<ExternalProcess>();
serviceCollection.AddScoped<CodeTools>();
serviceCollection.AddScoped<DirectoryPacker>();
serviceCollection.AddScoped<FileFinder>();
serviceCollection.AddScoped<CsNonStandardTypeExtractorPlugin>();
serviceCollection.AddScoped<TsNonStandardModuleExtractorPlugin>();
serviceCollection.AddScoped<FileIO>();
serviceCollection.AddScoped<VSCode>();
serviceCollection.AddScoped<AutoFixLlmAnswer>();
serviceCollection.AddScoped<WorkingDirectory>();
serviceCollection.AddScoped<IConfiguration>(provider => configuration);

serviceCollection.AddScoped<CodeAgent>();

var openAILink = Environment.GetEnvironmentVariable("MYAI_URI");
var openAIKey = Environment.GetEnvironmentVariable("MYAI_KEY");
var openAIModel = Environment.GetEnvironmentVariable("MYAI_MODEL");

var options = configuration.GetSection("Model").Get<ModelOptions>();

IChatClient client = options != null
    ? new OpenAIChatClient(new OpenAI.OpenAIClient(new ApiKeyCredential(options.ApiKey), new OpenAI.OpenAIClientOptions { Endpoint = new Uri(options.Endpoint) }), options.ModelId)
    : new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.2");

var chatClient = openAILink is not null && openAIKey is not null && openAIModel is not null
    ? new AzureOpenAIClient(new Uri(openAILink), new System.ClientModel.ApiKeyCredential(openAIKey)).AsChatClient(openAIModel)
    : client;

serviceCollection.AddChatClient(services =>
    new ChatClientBuilder(chatClient)
        .UseLogging(services.GetRequiredService<ILoggerFactory>())
        .UseFunctionInvocation(services.GetRequiredService<ILoggerFactory>())
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

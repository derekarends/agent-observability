using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Observability.Console.SingleAgent;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("env.local.json", optional: false, reloadOnChange: true)
    .Build();

var model = configuration["model"];
var azureEndpoint = configuration["azureEndpoint"];
var apiKey = configuration["apiKey"];

var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(model, azureEndpoint, apiKey);
builder.Services.AddLangfuseLogging(configuration);
//builder.Services.AddAspireLogging(configuration);


Kernel kernel = builder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

kernel.Plugins.AddFromType<LightsPlugin>("Lights");

var executionSettings = new OpenAIPromptExecutionSettings()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var history = new ChatHistory();

string? userInput;
while (true)
{
    Console.Write("User > ");
    userInput = Console.ReadLine();
    if (userInput is null || string.Equals(userInput.Trim(), "exit", StringComparison.OrdinalIgnoreCase))
        break;

    history.AddUserMessage(userInput);

    var result = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings,
        kernel);

    Console.WriteLine("Assistant > " + result);
    history.AddMessage(result.Role, result.Content ?? string.Empty);
}
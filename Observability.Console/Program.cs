using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Observability.Console.SingleAgent;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("env.local.json", optional: false, reloadOnChange: true)
    .Build();

var model = configuration["model"];
var azureEndpoint = configuration["azureEndpoint"];
var apiKey = configuration["apiKey"];
var aspireEndpoint = configuration["aspireEndpoint"];


AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService("Aspire");

using var traceProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource("Microsoft.SemanticKernel*")
    .AddOtlpExporter(options => options.Endpoint = new Uri(aspireEndpoint))
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddMeter("Microsoft.SemanticKernel*")
    .AddOtlpExporter(options => options.Endpoint = new Uri(aspireEndpoint))
    .Build();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    // Add OpenTelemetry as a logging provider
    builder.AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(resourceBuilder);
        options.AddOtlpExporter(options => options.Endpoint = new Uri(aspireEndpoint));
        // Format log messages. This is default to false.
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(model, azureEndpoint, apiKey);
builder.Services.AddSingleton(loggerFactory);

//var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(model, azureEndpoint, apiKey);
//builder.Services.AddLogging(loggingBuilder =>
//{
//    loggingBuilder.AddConsole();
//    loggingBuilder.SetMinimumLevel(LogLevel.Information);
//});

//builder.Services.AddSingleton<IFunctionInvocationFilter, LoggingFilter>(sp =>
//{
//    var logger = sp.GetRequiredService<ILogger<LoggingFilter>>();
//    return new LoggingFilter(logger);
//});

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
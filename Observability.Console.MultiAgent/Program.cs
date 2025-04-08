using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Observability.Console.MultiAgent;
using Microsoft.SemanticKernel.Agents;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;


#pragma warning disable SKEXP0001, SKEXP0110


var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("env.local.json", optional: false, reloadOnChange: true)
    .Build();

var model = configuration["model"];
var azureEndpoint = configuration["azureEndpoint"];
var apiKey = configuration["apiKey"];
var aspireEndpoint = configuration["aspireEndpoint"];
var appInsights = configuration["appInsights"];

// Enable model diagnostics with sensitive data.
AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

//var resourceBuilder = ResourceBuilder
//    .CreateDefault()
//    .AddService("AppInsights");

//using var traceProvider = Sdk.CreateTracerProviderBuilder()
//    .SetResourceBuilder(resourceBuilder)
//    .AddSource("Microsoft.SemanticKernel*")
//    .AddAzureMonitorTraceExporter(options => options.ConnectionString = appInsights)
//    .Build();

//using var meterProvider = Sdk.CreateMeterProviderBuilder()
//    .SetResourceBuilder(resourceBuilder)
//    .AddMeter("Microsoft.SemanticKernel*")
//    .AddAzureMonitorMetricExporter(options => options.ConnectionString = appInsights)
//    .Build();

//using var loggerFactory = LoggerFactory.Create(builder =>
//{
//    // Add OpenTelemetry as a logging provider
//    builder.AddOpenTelemetry(options =>
//    {
//        options.SetResourceBuilder(resourceBuilder);
//        options.AddAzureMonitorLogExporter(options => options.ConnectionString = appInsights);
//        // Format log messages. This is default to false.
//        options.IncludeFormattedMessage = true;
//        options.IncludeScopes = true;
//    });
//    builder.SetMinimumLevel(LogLevel.Debug);
//});

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

Kernel kernel = builder.Build();

var artDirectorAgent = new DirectorAgent(kernel).Create();
var copywriterAgent = new CopyWriterAgent(kernel).Create();
var chat = new AgentGroupChat(artDirectorAgent, copywriterAgent)
{
    ExecutionSettings =
    {
        TerminationStrategy = { MaximumIterations = 5 },
    }
};

string? userInput;
while (true)
{
    Console.Write("User > ");
    userInput = Console.ReadLine();
    if (userInput is null || string.Equals(userInput.Trim(), "exit", StringComparison.OrdinalIgnoreCase))
        break;

    chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userInput));
    await foreach (var response in chat.InvokeAsync())
    {
        Console.WriteLine($"\n# {response.Role} – {response.AuthorName ?? "*"}: '{response.Content}'");
    }
}

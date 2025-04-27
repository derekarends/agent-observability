using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Observability.Console.SingleAgent;

public static class ServiceExtensions
{
    public static IServiceCollection AddConsoleLogging(this IServiceCollection services)
    {
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConsole();
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<IFunctionInvocationFilter, LoggingFilter>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<LoggingFilter>>();
            return new LoggingFilter(logger);
        });

        return services;
    }

    public static IServiceCollection AddAspireLogging(this IServiceCollection services, IConfiguration configuration)
    {
        var aspireEndpoint = configuration["aspireEndpoint"];

        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService("Aspire");

        AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

        Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource("Microsoft.SemanticKernel*")
            .AddOtlpExporter(options => options.Endpoint = new Uri(aspireEndpoint))
            .Build();

        Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddMeter("Microsoft.SemanticKernel*")
            .AddOtlpExporter(options => options.Endpoint = new Uri(aspireEndpoint))
            .Build();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(resourceBuilder);
                options.AddOtlpExporter(options => options.Endpoint = new Uri(aspireEndpoint));

                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.ParseStateValues = true;
            });
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        services.AddSingleton(loggerFactory);
        return services;
    }

    public static IServiceCollection AddLangfuseLogging(this IServiceCollection services, IConfiguration configuration)
    {
        var langFuse = $"{configuration["langfuseEndpoint"]}/api/public/otel";
        var publicKey = configuration["langfusePublicKey"];
        var secretKey = configuration["langfuseSecretKey"];

        var langfuseAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{publicKey}:{secretKey}"));
        var authHeader = $"Authorization=Basic {langfuseAuth}";
        var endpoint = new Uri(langFuse);

        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService("Langfuse");

        AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource("Microsoft.SemanticKernel*")
            .AddOtlpExporter(o =>
            {
                o.Headers = authHeader;
                o.Endpoint = endpoint;
                o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                o.ExportProcessorType = ExportProcessorType.Simple;
            })
            .Build();

        var meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddMeter("Microsoft.SemanticKernel*")
            .AddOtlpExporter(o =>
            {
                o.Headers = authHeader;
                o.Endpoint = endpoint;
                o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                o.ExportProcessorType = ExportProcessorType.Simple;
            })
            .Build();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(resourceBuilder);
                options.AddOtlpExporter(o =>
                {
                    o.Headers = authHeader;
                    o.Endpoint = endpoint;
                    o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    o.ExportProcessorType = ExportProcessorType.Simple;
                });

                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.ParseStateValues = true;
            });
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        services.AddSingleton(loggerFactory);
        return services;
    }
}
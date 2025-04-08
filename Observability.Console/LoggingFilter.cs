using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Diagnostics;
using System.Text.Json;

namespace Observability.Console.SingleAgent;

public sealed class LoggingFilter(ILogger logger) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var args = JsonSerializer.Serialize(context.Arguments);
            logger.LogInformation($"Filter Before:[{context.Function.PluginName}.{context.Function.Name}] : {args}");
            await next(context);

            var elapsedTime = GetElapsedTime(stopwatch);
            logger.LogInformation($"Filter After:[{context.Function.PluginName}.{context.Function.Name}] : {context.Result} : {elapsedTime}");
        }
        catch (Exception ex)
        {
            var elapsedTime = GetElapsedTime(stopwatch);
            logger.LogError(ex, $"Error:[{context.Function.PluginName}.{context.Function.Name}] : {ex.Message} : {elapsedTime}");

            // Example: override function result value
            //context.Result = new FunctionResult(context.Function, "Friendly message instead of exception");
            throw;
        }
    }

    private static string GetElapsedTime(Stopwatch stopwatch)
    {
        stopwatch.Stop();

        TimeSpan ts = stopwatch.Elapsed;
        var elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
        return elapsedTime;
    }
}
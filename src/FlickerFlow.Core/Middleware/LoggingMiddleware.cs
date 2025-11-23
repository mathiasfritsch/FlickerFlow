using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Middleware;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlickerFlow.Core.Middleware;

/// <summary>
/// Middleware that logs message processing events with correlation IDs
/// </summary>
public class LoggingMiddleware : IConsumeMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(ConsumeContext context, Func<Task> next)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Started consuming message {MessageId} (CorrelationId: {CorrelationId})",
            context.MessageId,
            context.CorrelationId);

        try
        {
            await next();
            
            stopwatch.Stop();
            
            _logger.LogInformation(
                "Successfully consumed message {MessageId} in {ElapsedMs}ms (CorrelationId: {CorrelationId})",
                context.MessageId,
                stopwatch.ElapsedMilliseconds,
                context.CorrelationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex,
                "Error consuming message {MessageId} after {ElapsedMs}ms (CorrelationId: {CorrelationId})",
                context.MessageId,
                stopwatch.ElapsedMilliseconds,
                context.CorrelationId);
            
            throw;
        }
    }
}

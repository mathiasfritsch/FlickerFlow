using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Middleware;
using System.Diagnostics;

namespace FlickerFlow.Core.Diagnostics;

/// <summary>
/// Middleware that emits diagnostic events for distributed tracing
/// </summary>
public class DiagnosticMiddleware : IConsumeMiddleware
{
    private static readonly DiagnosticSource DiagnosticSource = 
        new DiagnosticListener(DiagnosticEvents.BaseName);

    public async Task Invoke(ConsumeContext context, Func<Task> next)
    {
        if (!DiagnosticSource.IsEnabled(DiagnosticEvents.ConsumeStart))
        {
            await next();
            return;
        }

        var activity = new Activity("FlickerFlow.Consume");
        
        // Set trace context from message headers if available
        if (context.Headers.TryGetHeader("traceparent", out var traceParent))
        {
            activity.SetParentId(traceParent?.ToString() ?? string.Empty);
        }

        activity.SetTag("messaging.system", "flickerflow");
        activity.SetTag("messaging.message_id", context.MessageId.ToString());
        activity.SetTag("messaging.correlation_id", context.CorrelationId?.ToString() ?? "");

        var stopwatch = Stopwatch.StartNew();

        // Emit start event
        if (DiagnosticSource.IsEnabled(DiagnosticEvents.ConsumeStart))
        {
            DiagnosticSource.Write(DiagnosticEvents.ConsumeStart, new
            {
                Context = context,
                Activity = activity,
                Timestamp = DateTime.UtcNow
            });
        }

        activity.Start();

        try
        {
            await next();
            
            stopwatch.Stop();
            activity.SetTag("messaging.operation_duration_ms", stopwatch.ElapsedMilliseconds);

            // Emit stop event
            if (DiagnosticSource.IsEnabled(DiagnosticEvents.ConsumeStop))
            {
                DiagnosticSource.Write(DiagnosticEvents.ConsumeStop, new
                {
                    Context = context,
                    Activity = activity,
                    Duration = stopwatch.Elapsed,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity.SetTag("messaging.operation_duration_ms", stopwatch.ElapsedMilliseconds);
            activity.SetTag("error", true);
            activity.SetTag("error.type", ex.GetType().FullName);

            // Emit error event
            if (DiagnosticSource.IsEnabled(DiagnosticEvents.ConsumeError))
            {
                DiagnosticSource.Write(DiagnosticEvents.ConsumeError, new
                {
                    Context = context,
                    Activity = activity,
                    Exception = ex,
                    Duration = stopwatch.Elapsed,
                    Timestamp = DateTime.UtcNow
                });
            }

            throw;
        }
        finally
        {
            activity.Stop();
        }
    }
}

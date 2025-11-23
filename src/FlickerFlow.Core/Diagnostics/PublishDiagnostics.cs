using System.Diagnostics;

namespace FlickerFlow.Core.Diagnostics;

/// <summary>
/// Helper class for emitting publish diagnostic events
/// </summary>
public static class PublishDiagnostics
{
    private static readonly DiagnosticSource DiagnosticSource = 
        new DiagnosticListener(DiagnosticEvents.BaseName);

    /// <summary>
    /// Emit diagnostic events for a publish operation
    /// </summary>
    public static async Task<T> TracePublish<T>(
        Guid messageId,
        Guid? correlationId,
        Type messageType,
        Func<Task<T>> operation)
    {
        if (!DiagnosticSource.IsEnabled(DiagnosticEvents.PublishStart))
        {
            return await operation();
        }

        var activity = new Activity("FlickerFlow.Publish");
        activity.SetTag("messaging.system", "flickerflow");
        activity.SetTag("messaging.operation", "publish");
        activity.SetTag("messaging.message_id", messageId.ToString());
        activity.SetTag("messaging.correlation_id", correlationId?.ToString() ?? "");
        activity.SetTag("messaging.message_type", messageType.FullName);

        var stopwatch = Stopwatch.StartNew();

        // Emit start event
        if (DiagnosticSource.IsEnabled(DiagnosticEvents.PublishStart))
        {
            DiagnosticSource.Write(DiagnosticEvents.PublishStart, new
            {
                MessageId = messageId,
                CorrelationId = correlationId,
                MessageType = messageType,
                Activity = activity,
                Timestamp = DateTime.UtcNow
            });
        }

        activity.Start();

        try
        {
            var result = await operation();
            
            stopwatch.Stop();
            activity.SetTag("messaging.operation_duration_ms", stopwatch.ElapsedMilliseconds);

            // Emit stop event
            if (DiagnosticSource.IsEnabled(DiagnosticEvents.PublishStop))
            {
                DiagnosticSource.Write(DiagnosticEvents.PublishStop, new
                {
                    MessageId = messageId,
                    CorrelationId = correlationId,
                    MessageType = messageType,
                    Activity = activity,
                    Duration = stopwatch.Elapsed,
                    Timestamp = DateTime.UtcNow
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity.SetTag("messaging.operation_duration_ms", stopwatch.ElapsedMilliseconds);
            activity.SetTag("error", true);
            activity.SetTag("error.type", ex.GetType().FullName);
            throw;
        }
        finally
        {
            activity.Stop();
        }
    }
}

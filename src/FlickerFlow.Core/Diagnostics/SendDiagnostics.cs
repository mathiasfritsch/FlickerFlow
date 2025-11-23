using System.Diagnostics;

namespace FlickerFlow.Core.Diagnostics;

/// <summary>
/// Helper class for emitting send diagnostic events
/// </summary>
public static class SendDiagnostics
{
    private static readonly DiagnosticSource DiagnosticSource = 
        new DiagnosticListener(DiagnosticEvents.BaseName);

    /// <summary>
    /// Emit diagnostic events for a send operation
    /// </summary>
    public static async Task<T> TraceSend<T>(
        Guid messageId,
        Guid? correlationId,
        Type messageType,
        Uri destinationAddress,
        Func<Task<T>> operation)
    {
        if (!DiagnosticSource.IsEnabled(DiagnosticEvents.SendStart))
        {
            return await operation();
        }

        var activity = new Activity("FlickerFlow.Send");
        activity.SetTag("messaging.system", "flickerflow");
        activity.SetTag("messaging.operation", "send");
        activity.SetTag("messaging.message_id", messageId.ToString());
        activity.SetTag("messaging.correlation_id", correlationId?.ToString() ?? "");
        activity.SetTag("messaging.message_type", messageType.FullName);
        activity.SetTag("messaging.destination", destinationAddress.ToString());

        var stopwatch = Stopwatch.StartNew();

        // Emit start event
        if (DiagnosticSource.IsEnabled(DiagnosticEvents.SendStart))
        {
            DiagnosticSource.Write(DiagnosticEvents.SendStart, new
            {
                MessageId = messageId,
                CorrelationId = correlationId,
                MessageType = messageType,
                DestinationAddress = destinationAddress,
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
            if (DiagnosticSource.IsEnabled(DiagnosticEvents.SendStop))
            {
                DiagnosticSource.Write(DiagnosticEvents.SendStop, new
                {
                    MessageId = messageId,
                    CorrelationId = correlationId,
                    MessageType = messageType,
                    DestinationAddress = destinationAddress,
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

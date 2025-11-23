namespace FlickerFlow.Abstractions;

/// <summary>
/// Context for sending messages
/// </summary>
public interface SendContext<out TMessage> where TMessage : class
{
    /// <summary>
    /// The message being sent
    /// </summary>
    TMessage Message { get; }

    /// <summary>
    /// Message headers
    /// </summary>
    Headers Headers { get; }

    /// <summary>
    /// Correlation identifier
    /// </summary>
    Guid? CorrelationId { get; set; }

    /// <summary>
    /// Destination address
    /// </summary>
    Uri DestinationAddress { get; }

    /// <summary>
    /// Cancellation token
    /// </summary>
    CancellationToken CancellationToken { get; }
}

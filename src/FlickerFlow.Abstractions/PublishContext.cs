namespace FlickerFlow.Abstractions;

/// <summary>
/// Context for publishing messages
/// </summary>
public interface PublishContext<out TMessage> where TMessage : class
{
    /// <summary>
    /// The message being published
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
    /// Cancellation token
    /// </summary>
    CancellationToken CancellationToken { get; }
}

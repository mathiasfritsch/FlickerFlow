namespace FlickerFlow.Abstractions;

/// <summary>
/// Base interface for consume context
/// </summary>
public interface ConsumeContext
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    Guid MessageId { get; }

    /// <summary>
    /// Correlation identifier for tracking related messages
    /// </summary>
    Guid? CorrelationId { get; }

    /// <summary>
    /// Timestamp when the message was sent
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Message headers
    /// </summary>
    Headers Headers { get; }

    /// <summary>
    /// Cancellation token for the operation
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Publish a message
    /// </summary>
    Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Send a message to a specific destination
    /// </summary>
    Task Send<T>(Uri destinationAddress, T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Respond to a request message
    /// </summary>
    Task RespondAsync<T>(T message) where T : class;
}

/// <summary>
/// Generic consume context with typed message
/// </summary>
public interface ConsumeContext<out TMessage> : ConsumeContext where TMessage : class
{
    /// <summary>
    /// The message being consumed
    /// </summary>
    TMessage Message { get; }
}

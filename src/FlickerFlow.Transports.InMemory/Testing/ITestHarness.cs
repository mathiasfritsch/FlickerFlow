namespace FlickerFlow.Transports.InMemory.Testing;

/// <summary>
/// Test harness interface for verifying message flow in tests
/// </summary>
public interface ITestHarness
{
    /// <summary>
    /// Wait for a published message of the specified type
    /// </summary>
    Task<PublishedMessage<T>?> Published<T>(TimeSpan? timeout = null) where T : class;

    /// <summary>
    /// Wait for a consumed message of the specified type
    /// </summary>
    Task<ConsumedMessage<T>?> Consumed<T>(TimeSpan? timeout = null) where T : class;

    /// <summary>
    /// Wait for a sent message of the specified type
    /// </summary>
    Task<SentMessage<T>?> Sent<T>(TimeSpan? timeout = null) where T : class;

    /// <summary>
    /// Get all published messages of the specified type
    /// </summary>
    IReadOnlyList<PublishedMessage<T>> GetPublished<T>() where T : class;

    /// <summary>
    /// Get all consumed messages of the specified type
    /// </summary>
    IReadOnlyList<ConsumedMessage<T>> GetConsumed<T>() where T : class;

    /// <summary>
    /// Get all sent messages of the specified type
    /// </summary>
    IReadOnlyList<SentMessage<T>> GetSent<T>() where T : class;

    /// <summary>
    /// Reset the test harness state
    /// </summary>
    void Reset();
}

/// <summary>
/// Represents a published message
/// </summary>
public class PublishedMessage<T> where T : class
{
    public T Message { get; }
    public DateTime Timestamp { get; }
    public Guid MessageId { get; }
    public Guid? CorrelationId { get; }

    public PublishedMessage(T message, DateTime timestamp, Guid messageId, Guid? correlationId)
    {
        Message = message;
        Timestamp = timestamp;
        MessageId = messageId;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Represents a consumed message
/// </summary>
public class ConsumedMessage<T> where T : class
{
    public T Message { get; }
    public DateTime Timestamp { get; }
    public Guid MessageId { get; }
    public Guid? CorrelationId { get; }
    public string EndpointName { get; }

    public ConsumedMessage(T message, DateTime timestamp, Guid messageId, Guid? correlationId, string endpointName)
    {
        Message = message;
        Timestamp = timestamp;
        MessageId = messageId;
        CorrelationId = correlationId;
        EndpointName = endpointName;
    }
}

/// <summary>
/// Represents a sent message
/// </summary>
public class SentMessage<T> where T : class
{
    public T Message { get; }
    public DateTime Timestamp { get; }
    public Guid MessageId { get; }
    public Guid? CorrelationId { get; }
    public Uri DestinationAddress { get; }

    public SentMessage(T message, DateTime timestamp, Guid messageId, Guid? correlationId, Uri destinationAddress)
    {
        Message = message;
        Timestamp = timestamp;
        MessageId = messageId;
        CorrelationId = correlationId;
        DestinationAddress = destinationAddress;
    }
}

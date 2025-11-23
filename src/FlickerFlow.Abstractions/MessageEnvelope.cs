namespace FlickerFlow.Abstractions;

/// <summary>
/// Internal representation of messages with metadata
/// </summary>
public class MessageEnvelope
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Correlation identifier for tracking related messages
    /// </summary>
    public Guid? CorrelationId { get; set; }

    /// <summary>
    /// Conversation identifier for request/response patterns
    /// </summary>
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// Timestamp when the message was sent
    /// </summary>
    public DateTime SentTime { get; set; }

    /// <summary>
    /// Fully qualified type name of the message
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Content type of the serialized payload
    /// </summary>
    public string ContentType { get; set; } = "application/json";

    /// <summary>
    /// Serialized message payload
    /// </summary>
    public byte[] Payload { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Message headers and metadata
    /// </summary>
    public Headers Headers { get; set; } = new();

    /// <summary>
    /// Source address where the message originated
    /// </summary>
    public string? SourceAddress { get; set; }

    /// <summary>
    /// Destination address for the message
    /// </summary>
    public string? DestinationAddress { get; set; }

    /// <summary>
    /// Address for response messages (request/response pattern)
    /// </summary>
    public string? ResponseAddress { get; set; }

    /// <summary>
    /// Address for fault messages (error handling)
    /// </summary>
    public string? FaultAddress { get; set; }

    /// <summary>
    /// Number of times this message has been retried
    /// </summary>
    public int RetryCount { get; set; }
}

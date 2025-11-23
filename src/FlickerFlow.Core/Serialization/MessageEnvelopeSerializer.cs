using FlickerFlow.Abstractions;

namespace FlickerFlow.Core.Serialization;

/// <summary>
/// Handles serialization and deserialization of message envelopes with type information
/// </summary>
public class MessageEnvelopeSerializer
{
    private readonly IMessageSerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the MessageEnvelopeSerializer
    /// </summary>
    /// <param name="serializer">The message serializer to use</param>
    public MessageEnvelopeSerializer(IMessageSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <summary>
    /// Creates a message envelope from a message, extracting type information
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to wrap</param>
    /// <param name="messageId">Optional message ID (generated if not provided)</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Message envelope with serialized payload and type information</returns>
    public MessageEnvelope CreateEnvelope<T>(
        T message,
        Guid? messageId = null,
        Guid? correlationId = null) where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();
        var typeName = MessageTypeCache.GetTypeName(messageType);
        var payload = _serializer.Serialize(message);

        return new MessageEnvelope
        {
            MessageId = messageId ?? Guid.NewGuid(),
            CorrelationId = correlationId,
            SentTime = DateTime.UtcNow,
            MessageType = typeName,
            ContentType = _serializer.ContentType,
            Payload = payload,
            Headers = new Headers()
        };
    }

    /// <summary>
    /// Deserializes a message from an envelope using the embedded type information
    /// </summary>
    /// <param name="envelope">The message envelope</param>
    /// <returns>Deserialized message</returns>
    public object DeserializeMessage(MessageEnvelope envelope)
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        if (string.IsNullOrWhiteSpace(envelope.MessageType))
            throw new InvalidOperationException("Message envelope does not contain type information");

        if (envelope.Payload == null || envelope.Payload.Length == 0)
            throw new InvalidOperationException("Message envelope does not contain payload data");

        var messageType = MessageTypeCache.GetType(envelope.MessageType);
        if (messageType == null)
            throw new InvalidOperationException(
                $"Unable to resolve message type: {envelope.MessageType}");

        return _serializer.Deserialize(envelope.Payload, messageType);
    }

    /// <summary>
    /// Deserializes a message from an envelope to a specific type
    /// </summary>
    /// <typeparam name="T">The expected message type</typeparam>
    /// <param name="envelope">The message envelope</param>
    /// <returns>Deserialized message</returns>
    public T DeserializeMessage<T>(MessageEnvelope envelope) where T : class
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        if (envelope.Payload == null || envelope.Payload.Length == 0)
            throw new InvalidOperationException("Message envelope does not contain payload data");

        // If type information is available, verify it matches the expected type
        if (!string.IsNullOrWhiteSpace(envelope.MessageType))
        {
            var messageType = MessageTypeCache.GetType(envelope.MessageType);
            if (messageType != null && !typeof(T).IsAssignableFrom(messageType))
            {
                throw new InvalidOperationException(
                    $"Message type {envelope.MessageType} is not assignable to {typeof(T).Name}");
            }
        }

        return _serializer.Deserialize<T>(envelope.Payload);
    }

    /// <summary>
    /// Updates an existing envelope with a new message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="envelope">The envelope to update</param>
    /// <param name="message">The new message</param>
    public void UpdateEnvelopePayload<T>(MessageEnvelope envelope, T message) where T : class
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();
        var typeName = MessageTypeCache.GetTypeName(messageType);
        var payload = _serializer.Serialize(message);

        envelope.MessageType = typeName;
        envelope.ContentType = _serializer.ContentType;
        envelope.Payload = payload;
    }
}

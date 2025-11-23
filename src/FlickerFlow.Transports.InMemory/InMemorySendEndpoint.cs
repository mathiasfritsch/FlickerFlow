using FlickerFlow.Abstractions;
using FlickerFlow.Transports.InMemory.Testing;

namespace FlickerFlow.Transports.InMemory;

/// <summary>
/// In-memory send endpoint for point-to-point messaging
/// </summary>
internal class InMemorySendEndpoint : ISendEndpoint
{
    private readonly InMemoryQueue _queue;
    private readonly IMessageSerializer _serializer;
    private readonly TestHarness _testHarness;

    public InMemorySendEndpoint(
        InMemoryQueue queue,
        IMessageSerializer serializer,
        TestHarness testHarness)
    {
        _queue = queue;
        _serializer = serializer;
        _testHarness = testHarness;
    }

    public async Task Send<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        var envelope = CreateEnvelope(message, null);
        await _queue.Enqueue(envelope, cancellationToken);
        
        // Track for test harness
        _testHarness.TrackSent(message, envelope.MessageId, envelope.CorrelationId, 
            new Uri($"inmemory://{_queue.Name}"));
    }

    public async Task Send<T>(T message, Action<SendContext<T>> configure, CancellationToken cancellationToken = default) where T : class
    {
        var destinationUri = new Uri($"inmemory://{_queue.Name}");
        var context = new SendContextImpl<T>(message, destinationUri, cancellationToken);
        configure(context);
        
        var envelope = CreateEnvelope(message, context);
        await _queue.Enqueue(envelope, cancellationToken);
        
        // Track for test harness
        _testHarness.TrackSent(message, envelope.MessageId, envelope.CorrelationId, destinationUri);
    }

    private MessageEnvelope CreateEnvelope<T>(T message, SendContextImpl<T>? context) where T : class
    {
        var messageType = message.GetType();
        var payload = _serializer.Serialize(message);

        var envelope = new MessageEnvelope
        {
            MessageId = context?.MessageId ?? Guid.NewGuid(),
            CorrelationId = context?.CorrelationId,
            ConversationId = context?.ConversationId,
            SentTime = DateTime.UtcNow,
            MessageType = $"{messageType.FullName}, {messageType.Assembly.GetName().Name}",
            ContentType = _serializer.ContentType,
            Payload = payload,
            Headers = context?.Headers ?? new Headers(),
            DestinationAddress = $"inmemory://{_queue.Name}"
        };

        return envelope;
    }
}

/// <summary>
/// Implementation of SendContext for configuring send operations
/// </summary>
internal class SendContextImpl<T> : SendContext<T> where T : class
{
    public SendContextImpl(T message, Uri destinationAddress, CancellationToken cancellationToken)
    {
        Message = message;
        MessageId = Guid.NewGuid();
        Headers = new Headers();
        DestinationAddress = destinationAddress;
        CancellationToken = cancellationToken;
    }

    public T Message { get; }
    public Guid MessageId { get; set; }
    public Guid? CorrelationId { get; set; }
    public Guid? ConversationId { get; set; }
    public Headers Headers { get; }
    public Uri DestinationAddress { get; }
    public CancellationToken CancellationToken { get; }
}

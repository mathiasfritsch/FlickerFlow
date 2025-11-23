using System.Collections.Concurrent;
using FlickerFlow.Abstractions;
using FlickerFlow.Transports.InMemory.Testing;

namespace FlickerFlow.Transports.InMemory;

/// <summary>
/// In-memory publish endpoint for broadcasting messages
/// </summary>
internal class InMemoryPublishEndpoint<TMessage> : IPublishEndpoint where TMessage : class
{
    private readonly ConcurrentDictionary<Type, List<InMemoryQueue>> _subscriptions;
    private readonly IMessageSerializer _serializer;
    private readonly TestHarness _testHarness;

    public InMemoryPublishEndpoint(
        ConcurrentDictionary<Type, List<InMemoryQueue>> subscriptions,
        IMessageSerializer serializer,
        TestHarness testHarness)
    {
        _subscriptions = subscriptions;
        _serializer = serializer;
        _testHarness = testHarness;
    }

    public async Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        var envelope = CreateEnvelope(message, null);
        await PublishToSubscribers(message, envelope, cancellationToken);
    }

    public async Task Publish<T>(T message, Action<PublishContext<T>> configure, CancellationToken cancellationToken = default) where T : class
    {
        var context = new PublishContextImpl<T>(message, cancellationToken);
        configure(context);
        
        var envelope = CreateEnvelope(message, context);
        await PublishToSubscribers(message, envelope, cancellationToken);
    }

    private async Task PublishToSubscribers<T>(T message, MessageEnvelope envelope, CancellationToken cancellationToken) where T : class
    {
        var messageType = message.GetType();
        
        // Track for test harness
        _testHarness.TrackPublished(message, envelope.MessageId, envelope.CorrelationId);

        // Get all subscriptions for this message type
        if (_subscriptions.TryGetValue(messageType, out var queues))
        {
            List<InMemoryQueue> queuesCopy;
            lock (queues)
            {
                queuesCopy = new List<InMemoryQueue>(queues);
            }

            // Deliver to all subscribers
            var tasks = queuesCopy.Select(queue => queue.Enqueue(envelope, cancellationToken));
            await Task.WhenAll(tasks);
        }
    }

    private MessageEnvelope CreateEnvelope<T>(T message, PublishContextImpl<T>? context) where T : class
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
            Headers = context?.Headers ?? new Headers()
        };

        return envelope;
    }
}

/// <summary>
/// Implementation of PublishContext for configuring publish operations
/// </summary>
internal class PublishContextImpl<T> : PublishContext<T> where T : class
{
    public PublishContextImpl(T message, CancellationToken cancellationToken)
    {
        Message = message;
        MessageId = Guid.NewGuid();
        Headers = new Headers();
        CancellationToken = cancellationToken;
    }

    public T Message { get; }
    public Guid MessageId { get; set; }
    public Guid? CorrelationId { get; set; }
    public Guid? ConversationId { get; set; }
    public Headers Headers { get; }
    public CancellationToken CancellationToken { get; }
}

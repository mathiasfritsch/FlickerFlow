using System.Collections.Concurrent;

namespace FlickerFlow.Transports.InMemory.Testing;

/// <summary>
/// Test harness implementation for tracking message flow
/// </summary>
public class TestHarness : ITestHarness
{
    private readonly ConcurrentBag<object> _publishedMessages = new();
    private readonly ConcurrentBag<object> _consumedMessages = new();
    private readonly ConcurrentBag<object> _sentMessages = new();
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);

    public async Task<PublishedMessage<T>?> Published<T>(TimeSpan? timeout = null) where T : class
    {
        var timeoutValue = timeout ?? _defaultTimeout;
        var endTime = DateTime.UtcNow.Add(timeoutValue);

        while (DateTime.UtcNow < endTime)
        {
            var message = _publishedMessages
                .OfType<PublishedMessage<T>>()
                .FirstOrDefault();

            if (message != null)
            {
                return message;
            }

            await Task.Delay(10);
        }

        return null;
    }

    public async Task<ConsumedMessage<T>?> Consumed<T>(TimeSpan? timeout = null) where T : class
    {
        var timeoutValue = timeout ?? _defaultTimeout;
        var endTime = DateTime.UtcNow.Add(timeoutValue);

        while (DateTime.UtcNow < endTime)
        {
            var message = _consumedMessages
                .OfType<ConsumedMessage<T>>()
                .FirstOrDefault();

            if (message != null)
            {
                return message;
            }

            await Task.Delay(10);
        }

        return null;
    }

    public async Task<SentMessage<T>?> Sent<T>(TimeSpan? timeout = null) where T : class
    {
        var timeoutValue = timeout ?? _defaultTimeout;
        var endTime = DateTime.UtcNow.Add(timeoutValue);

        while (DateTime.UtcNow < endTime)
        {
            var message = _sentMessages
                .OfType<SentMessage<T>>()
                .FirstOrDefault();

            if (message != null)
            {
                return message;
            }

            await Task.Delay(10);
        }

        return null;
    }

    public IReadOnlyList<PublishedMessage<T>> GetPublished<T>() where T : class
    {
        return _publishedMessages
            .OfType<PublishedMessage<T>>()
            .ToList();
    }

    public IReadOnlyList<ConsumedMessage<T>> GetConsumed<T>() where T : class
    {
        return _consumedMessages
            .OfType<ConsumedMessage<T>>()
            .ToList();
    }

    public IReadOnlyList<SentMessage<T>> GetSent<T>() where T : class
    {
        return _sentMessages
            .OfType<SentMessage<T>>()
            .ToList();
    }

    public void Reset()
    {
        _publishedMessages.Clear();
        _consumedMessages.Clear();
        _sentMessages.Clear();
    }

    internal void TrackPublished<T>(T message, Guid messageId, Guid? correlationId) where T : class
    {
        var publishedMessage = new PublishedMessage<T>(message, DateTime.UtcNow, messageId, correlationId);
        _publishedMessages.Add(publishedMessage);
    }

    internal void TrackConsumed<T>(T message, Guid messageId, Guid? correlationId, string endpointName) where T : class
    {
        var consumedMessage = new ConsumedMessage<T>(message, DateTime.UtcNow, messageId, correlationId, endpointName);
        _consumedMessages.Add(consumedMessage);
    }

    internal void TrackSent<T>(T message, Guid messageId, Guid? correlationId, Uri destinationAddress) where T : class
    {
        var sentMessage = new SentMessage<T>(message, DateTime.UtcNow, messageId, correlationId, destinationAddress);
        _sentMessages.Add(sentMessage);
    }
}

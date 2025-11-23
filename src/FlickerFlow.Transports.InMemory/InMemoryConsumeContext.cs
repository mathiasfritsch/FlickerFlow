using FlickerFlow.Abstractions;

namespace FlickerFlow.Transports.InMemory;

/// <summary>
/// In-memory implementation of ConsumeContext
/// </summary>
internal class InMemoryConsumeContext<TMessage> : ConsumeContext<TMessage> where TMessage : class
{
    private readonly ITransport _transport;

    public InMemoryConsumeContext(
        TMessage message,
        MessageEnvelope envelope,
        ITransport transport,
        CancellationToken cancellationToken)
    {
        Message = message;
        MessageId = envelope.MessageId;
        CorrelationId = envelope.CorrelationId;
        Timestamp = envelope.SentTime;
        Headers = envelope.Headers;
        CancellationToken = cancellationToken;
        _transport = transport;
        ResponseAddress = envelope.ResponseAddress;
    }

    public TMessage Message { get; }
    public Guid MessageId { get; }
    public Guid? CorrelationId { get; }
    public DateTime Timestamp { get; }
    public Headers Headers { get; }
    public CancellationToken CancellationToken { get; }
    
    private string? ResponseAddress { get; }

    public async Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        var publishEndpoint = await _transport.GetPublishEndpoint<T>();
        await publishEndpoint.Publish(message, cancellationToken);
    }

    public async Task Send<T>(Uri destinationAddress, T message, CancellationToken cancellationToken = default) where T : class
    {
        var sendEndpoint = await _transport.GetSendEndpoint(destinationAddress);
        await sendEndpoint.Send(message, cancellationToken);
    }

    public async Task RespondAsync<T>(T message) where T : class
    {
        if (string.IsNullOrEmpty(ResponseAddress))
        {
            throw new InvalidOperationException("Cannot respond to a message that does not have a response address");
        }

        var responseUri = new Uri(ResponseAddress);
        await Send(responseUri, message, CancellationToken);
    }
}

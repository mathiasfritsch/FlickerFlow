using FlickerFlow.Abstractions;

namespace FlickerFlow.Core.Context;

/// <summary>
/// Implementation of ConsumeContext with typed message
/// </summary>
internal class ConsumeContextImpl<TMessage> : ConsumeContext<TMessage> where TMessage : class
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ISendEndpointProvider _sendEndpointProvider;

    public ConsumeContextImpl(
        TMessage message,
        MessageEnvelope envelope,
        IPublishEndpoint publishEndpoint,
        ISendEndpointProvider sendEndpointProvider,
        CancellationToken cancellationToken)
    {
        Message = message;
        MessageId = envelope.MessageId;
        CorrelationId = envelope.CorrelationId;
        Timestamp = envelope.SentTime;
        Headers = envelope.Headers;
        CancellationToken = cancellationToken;
        _publishEndpoint = publishEndpoint;
        _sendEndpointProvider = sendEndpointProvider;
        ResponseAddress = envelope.ResponseAddress;
    }

    public TMessage Message { get; }
    public Guid MessageId { get; }
    public Guid? CorrelationId { get; }
    public DateTime Timestamp { get; }
    public Headers Headers { get; }
    public CancellationToken CancellationToken { get; }
    
    private string? ResponseAddress { get; }

    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        return _publishEndpoint.Publish(message, cancellationToken);
    }

    public async Task Send<T>(Uri destinationAddress, T message, CancellationToken cancellationToken = default) where T : class
    {
        var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(destinationAddress);
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

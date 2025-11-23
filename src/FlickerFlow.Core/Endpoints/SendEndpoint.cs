using FlickerFlow.Abstractions;
using FlickerFlow.Core.Context;

namespace FlickerFlow.Core.Endpoints;

/// <summary>
/// Implementation of ISendEndpoint for point-to-point messaging
/// </summary>
internal class SendEndpoint : ISendEndpoint
{
    private readonly Uri _address;
    private readonly ITransport _transport;

    public SendEndpoint(Uri address, ITransport transport)
    {
        _address = address;
        _transport = transport;
    }

    public async Task Send<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        var sendEndpoint = await _transport.GetSendEndpoint(_address);
        await sendEndpoint.Send(message, cancellationToken);
    }

    public async Task Send<T>(T message, Action<SendContext<T>> configure, CancellationToken cancellationToken = default) where T : class
    {
        var context = new SendContextImpl<T>(message, _address, cancellationToken);
        configure(context);
        
        var sendEndpoint = await _transport.GetSendEndpoint(_address);
        await sendEndpoint.Send(message, ctx =>
        {
            ctx.CorrelationId = context.CorrelationId;
            foreach (var header in context.Headers)
            {
                ctx.Headers.Set(header.Key, header.Value);
            }
        }, cancellationToken);
    }
}

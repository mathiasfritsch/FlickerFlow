using FlickerFlow.Abstractions;
using FlickerFlow.Core.Context;

namespace FlickerFlow.Core.Endpoints;

/// <summary>
/// Implementation of IPublishEndpoint for message broadcasting
/// </summary>
internal class PublishEndpoint : IPublishEndpoint
{
    private readonly ITransport _transport;

    public PublishEndpoint(ITransport transport)
    {
        _transport = transport;
    }

    public async Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        var publishEndpoint = await _transport.GetPublishEndpoint<T>();
        await publishEndpoint.Publish(message, cancellationToken);
    }

    public async Task Publish<T>(T message, Action<PublishContext<T>> configure, CancellationToken cancellationToken = default) where T : class
    {
        var context = new PublishContextImpl<T>(message, cancellationToken);
        configure(context);
        
        var publishEndpoint = await _transport.GetPublishEndpoint<T>();
        await publishEndpoint.Publish(message, ctx =>
        {
            ctx.CorrelationId = context.CorrelationId;
            foreach (var header in context.Headers)
            {
                ctx.Headers.Set(header.Key, header.Value);
            }
        }, cancellationToken);
    }
}

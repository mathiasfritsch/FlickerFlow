using FlickerFlow.Abstractions;

namespace FlickerFlow.Core.Context;

/// <summary>
/// Implementation of PublishContext
/// </summary>
internal class PublishContextImpl<TMessage> : PublishContext<TMessage> where TMessage : class
{
    public PublishContextImpl(TMessage message, CancellationToken cancellationToken)
    {
        Message = message;
        Headers = new Headers();
        CancellationToken = cancellationToken;
    }

    public TMessage Message { get; }
    public Headers Headers { get; }
    public Guid? CorrelationId { get; set; }
    public CancellationToken CancellationToken { get; }
}

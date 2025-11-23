using FlickerFlow.Abstractions;

namespace FlickerFlow.Core.Context;

/// <summary>
/// Implementation of SendContext
/// </summary>
internal class SendContextImpl<TMessage> : SendContext<TMessage> where TMessage : class
{
    public SendContextImpl(TMessage message, Uri destinationAddress, CancellationToken cancellationToken)
    {
        Message = message;
        DestinationAddress = destinationAddress;
        Headers = new Headers();
        CancellationToken = cancellationToken;
    }

    public TMessage Message { get; }
    public Headers Headers { get; }
    public Guid? CorrelationId { get; set; }
    public Uri DestinationAddress { get; }
    public CancellationToken CancellationToken { get; }
}

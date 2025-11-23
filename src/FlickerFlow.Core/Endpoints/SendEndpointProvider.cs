using System.Collections.Concurrent;
using FlickerFlow.Abstractions;

namespace FlickerFlow.Core.Endpoints;

/// <summary>
/// Provider for send endpoints with caching
/// </summary>
internal class SendEndpointProvider : ISendEndpointProvider
{
    private readonly ITransport _transport;
    private readonly ConcurrentDictionary<Uri, ISendEndpoint> _endpointCache = new();

    public SendEndpointProvider(ITransport transport)
    {
        _transport = transport;
    }

    public Task<ISendEndpoint> GetSendEndpoint(Uri address)
    {
        var endpoint = _endpointCache.GetOrAdd(address, addr => new SendEndpoint(addr, _transport));
        return Task.FromResult(endpoint);
    }
}

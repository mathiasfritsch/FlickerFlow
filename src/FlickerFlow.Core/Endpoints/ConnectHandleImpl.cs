using FlickerFlow.Abstractions;

namespace FlickerFlow.Core.Endpoints;

/// <summary>
/// Handle for managing endpoint connections
/// </summary>
internal class ConnectHandleImpl : ConnectHandle
{
    private readonly IReceiveEndpoint _endpoint;
    private readonly Action<IReceiveEndpoint> _onDisconnect;
    private bool _disposed;

    public ConnectHandleImpl(IReceiveEndpoint endpoint, Action<IReceiveEndpoint> onDisconnect)
    {
        _endpoint = endpoint;
        _onDisconnect = onDisconnect;
    }

    public async Task Disconnect()
    {
        if (_disposed)
            return;

        await _endpoint.StopAsync();
        _onDisconnect(_endpoint);
        _disposed = true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Disconnect().GetAwaiter().GetResult();
    }
}

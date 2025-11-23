namespace FlickerFlow.Abstractions;

/// <summary>
/// Interface for point-to-point messaging
/// </summary>
public interface ISendEndpoint
{
    /// <summary>
    /// Send a message to a specific endpoint
    /// </summary>
    Task Send<T>(T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Send a message with additional configuration
    /// </summary>
    Task Send<T>(T message, Action<SendContext<T>> configure, CancellationToken cancellationToken = default) where T : class;
}

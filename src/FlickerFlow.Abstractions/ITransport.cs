namespace FlickerFlow.Abstractions;

/// <summary>
/// Base interface for all transport implementations
/// </summary>
public interface ITransport
{
    /// <summary>
    /// Get a send endpoint for the specified address
    /// </summary>
    Task<ISendEndpoint> GetSendEndpoint(Uri address);

    /// <summary>
    /// Get a publish endpoint for the specified message type
    /// </summary>
    Task<IPublishEndpoint> GetPublishEndpoint<T>() where T : class;

    /// <summary>
    /// Create a receive endpoint for consuming messages
    /// </summary>
    IReceiveEndpoint CreateReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configure);

    /// <summary>
    /// Start the transport and begin processing messages
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the transport and cease processing messages
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

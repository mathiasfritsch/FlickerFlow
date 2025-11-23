namespace FlickerFlow.Abstractions;

/// <summary>
/// Central interface for all messaging operations
/// </summary>
public interface IBus : IPublishEndpoint, ISendEndpointProvider
{
    /// <summary>
    /// Start the bus and begin processing messages
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the bus and cease processing messages
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Connect a receive endpoint to the bus
    /// </summary>
    ConnectHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configure);
}

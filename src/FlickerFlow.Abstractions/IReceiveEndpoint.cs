namespace FlickerFlow.Abstractions;

/// <summary>
/// Represents a queue or subscription that receives messages
/// </summary>
public interface IReceiveEndpoint
{
    /// <summary>
    /// Name of the endpoint
    /// </summary>
    string EndpointName { get; }

    /// <summary>
    /// Start receiving and processing messages
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop receiving and processing messages
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

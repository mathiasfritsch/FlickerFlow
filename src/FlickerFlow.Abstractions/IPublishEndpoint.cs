namespace FlickerFlow.Abstractions;

/// <summary>
/// Interface for broadcasting messages to multiple subscribers
/// </summary>
public interface IPublishEndpoint
{
    /// <summary>
    /// Publish a message to all subscribers
    /// </summary>
    Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Publish a message with additional configuration
    /// </summary>
    Task Publish<T>(T message, Action<PublishContext<T>> configure, CancellationToken cancellationToken = default) where T : class;
}

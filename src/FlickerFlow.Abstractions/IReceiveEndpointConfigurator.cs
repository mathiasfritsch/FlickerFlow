namespace FlickerFlow.Abstractions;

/// <summary>
/// Configurator for receive endpoints
/// </summary>
public interface IReceiveEndpointConfigurator
{
    /// <summary>
    /// Register a consumer type
    /// </summary>
    void Consumer<T>() where T : class, IConsumer;

    /// <summary>
    /// Register a consumer with a factory
    /// </summary>
    void Consumer<T>(Func<IServiceProvider, T> factory) where T : class, IConsumer;
}

/// <summary>
/// Marker interface for consumers
/// </summary>
public interface IConsumer
{
}

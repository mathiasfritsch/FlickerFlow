using FlickerFlow.Abstractions.Middleware;

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

    /// <summary>
    /// Add middleware to the consume pipeline
    /// </summary>
    void UseMiddleware<T>() where T : IConsumeMiddleware;

    /// <summary>
    /// Add middleware instance to the consume pipeline
    /// </summary>
    void UseMiddleware(IConsumeMiddleware middleware);

    /// <summary>
    /// Configure prefetch count for message retrieval
    /// </summary>
    void ConfigurePrefetchCount(int count);

    /// <summary>
    /// Configure concurrent message processing limit
    /// </summary>
    void ConfigureConcurrentMessageLimit(int limit);
}

/// <summary>
/// Marker interface for consumers
/// </summary>
public interface IConsumer
{
}

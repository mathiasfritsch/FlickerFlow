using System.Reflection;

namespace FlickerFlow.Abstractions.Configuration;

/// <summary>
/// Fluent API for configuring the bus
/// </summary>
public interface IBusConfigurator
{
    /// <summary>
    /// Register a consumer type
    /// </summary>
    void AddConsumer<T>() where T : class, IConsumer;

    /// <summary>
    /// Register consumers from an assembly
    /// </summary>
    void AddConsumers(Assembly assembly);

    /// <summary>
    /// Configure the bus to use RabbitMQ transport
    /// </summary>
    void UsingRabbitMq(Action<IRabbitMqBusFactoryConfigurator> configure);

    /// <summary>
    /// Configure the bus to use in-memory transport
    /// </summary>
    void UsingInMemory(Action<IInMemoryBusFactoryConfigurator> configure);

    /// <summary>
    /// Configure shutdown timeout
    /// </summary>
    void ConfigureShutdownTimeout(TimeSpan timeout);
}

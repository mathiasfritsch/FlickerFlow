namespace FlickerFlow.Abstractions.Configuration;

/// <summary>
/// Base interface for transport-specific bus factory configurators
/// </summary>
public interface IBusFactoryConfigurator
{
    /// <summary>
    /// Configure a receive endpoint
    /// </summary>
    void ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configure);
}

/// <summary>
/// RabbitMQ-specific bus factory configurator
/// </summary>
public interface IRabbitMqBusFactoryConfigurator : IBusFactoryConfigurator
{
    /// <summary>
    /// Configure RabbitMQ host connection
    /// </summary>
    void Host(string host, Action<IRabbitMqHostConfigurator>? configure = null);
}

/// <summary>
/// RabbitMQ host configurator
/// </summary>
public interface IRabbitMqHostConfigurator
{
    /// <summary>
    /// Set the port
    /// </summary>
    void Port(int port);

    /// <summary>
    /// Set the virtual host
    /// </summary>
    void VirtualHost(string virtualHost);

    /// <summary>
    /// Set credentials
    /// </summary>
    void Credentials(string username, string password);
}

/// <summary>
/// In-memory transport bus factory configurator
/// </summary>
public interface IInMemoryBusFactoryConfigurator : IBusFactoryConfigurator
{
}

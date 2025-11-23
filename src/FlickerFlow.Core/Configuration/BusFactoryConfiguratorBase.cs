using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Configuration;

namespace FlickerFlow.Core.Configuration;

/// <summary>
/// Base class for transport-specific bus factory configurators
/// </summary>
internal abstract class BusFactoryConfiguratorBase : IBusFactoryConfigurator
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly List<ReceiveEndpointSpec> ReceiveEndpoints = new();

    protected BusFactoryConfiguratorBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public void ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configure)
    {
        var endpointSpec = new ReceiveEndpointSpec(queueName, configure);
        ReceiveEndpoints.Add(endpointSpec);
    }

    public abstract ITransport Build();
}

/// <summary>
/// Stores receive endpoint specification
/// </summary>
internal class ReceiveEndpointSpec
{
    public string QueueName { get; }
    public Action<IReceiveEndpointConfigurator> Configure { get; }

    public ReceiveEndpointSpec(string queueName, Action<IReceiveEndpointConfigurator> configure)
    {
        QueueName = queueName;
        Configure = configure;
    }
}

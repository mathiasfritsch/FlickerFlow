using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Configuration;

namespace FlickerFlow.Core.Configuration;

/// <summary>
/// In-memory transport bus factory configurator
/// </summary>
internal class InMemoryBusFactoryConfigurator : BusFactoryConfiguratorBase, IInMemoryBusFactoryConfigurator
{
    public InMemoryBusFactoryConfigurator(IServiceProvider serviceProvider) 
        : base(serviceProvider)
    {
    }

    public override ITransport Build()
    {
        // Create the in-memory transport with the configured receive endpoints
        var transportType = Type.GetType("FlickerFlow.Transports.InMemory.InMemoryTransport, FlickerFlow.Transports.InMemory");
        
        if (transportType == null)
        {
            throw new InvalidOperationException(
                "FlickerFlow.Transports.InMemory assembly not found. " +
                "Please add a reference to FlickerFlow.Transports.InMemory package.");
        }

        var transport = Activator.CreateInstance(transportType, ServiceProvider, ReceiveEndpoints) as ITransport;
        
        if (transport == null)
        {
            throw new InvalidOperationException("Failed to create InMemoryTransport instance");
        }

        return transport;
    }
}

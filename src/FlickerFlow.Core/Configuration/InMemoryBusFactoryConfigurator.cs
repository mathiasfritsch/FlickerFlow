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
        // This will be implemented when we create the in-memory transport
        // For now, throw to indicate it's not yet implemented
        throw new NotImplementedException("In-memory transport will be implemented in task 6");
    }
}

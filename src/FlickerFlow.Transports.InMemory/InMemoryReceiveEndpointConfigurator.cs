using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Middleware;

namespace FlickerFlow.Transports.InMemory;

/// <summary>
/// In-memory implementation of receive endpoint configurator
/// </summary>
internal class InMemoryReceiveEndpointConfigurator : IReceiveEndpointConfigurator
{
    private readonly List<Type> _consumerTypes;
    private readonly Action<int> _setConcurrentMessageLimit;

    public InMemoryReceiveEndpointConfigurator(
        List<Type> consumerTypes,
        Action<int> setConcurrentMessageLimit)
    {
        _consumerTypes = consumerTypes;
        _setConcurrentMessageLimit = setConcurrentMessageLimit;
    }

    public void Consumer<T>() where T : class, IConsumer
    {
        var consumerType = typeof(T);
        if (!_consumerTypes.Contains(consumerType))
        {
            _consumerTypes.Add(consumerType);
        }
    }

    public void Consumer<T>(Func<IServiceProvider, T> factory) where T : class, IConsumer
    {
        // For in-memory transport, we'll just register the type
        // The factory pattern is handled by DI
        Consumer<T>();
    }

    public void UseMiddleware<T>() where T : IConsumeMiddleware
    {
        // Middleware support can be added later if needed
        // For now, we'll skip middleware in the in-memory transport
    }

    public void UseMiddleware(IConsumeMiddleware middleware)
    {
        // Middleware support can be added later if needed
    }

    public void ConfigurePrefetchCount(int count)
    {
        // Not applicable for in-memory transport
    }

    public void ConfigureConcurrentMessageLimit(int limit)
    {
        _setConcurrentMessageLimit(limit);
    }
}

using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Middleware;

namespace FlickerFlow.Core.Endpoints;

/// <summary>
/// Configuration for a receive endpoint
/// </summary>
internal class ReceiveEndpointConfiguration : IReceiveEndpointConfigurator
{
    private readonly List<ConsumerRegistration> _consumers = new();
    private readonly List<IConsumeMiddleware> _middlewares = new();

    public int PrefetchCount { get; private set; } = 16;
    public int ConcurrentMessageLimit { get; private set; } = 10;

    public IReadOnlyList<ConsumerRegistration> Consumers => _consumers;
    public IReadOnlyList<IConsumeMiddleware> Middlewares => _middlewares;

    public void Consumer<T>() where T : class, IConsumer
    {
        _consumers.Add(new ConsumerRegistration(typeof(T), null));
    }

    public void Consumer<T>(Func<IServiceProvider, T> factory) where T : class, IConsumer
    {
        _consumers.Add(new ConsumerRegistration(typeof(T), sp => factory(sp)));
    }

    public void UseMiddleware<T>() where T : IConsumeMiddleware
    {
        // Middleware will be resolved from DI when needed
        _consumers.Add(new ConsumerRegistration(typeof(T), null, isMiddleware: true));
    }

    public void UseMiddleware(IConsumeMiddleware middleware)
    {
        _middlewares.Add(middleware);
    }

    public void ConfigurePrefetchCount(int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Prefetch count must be greater than zero");
        
        PrefetchCount = count;
    }

    public void ConfigureConcurrentMessageLimit(int limit)
    {
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit), "Concurrent message limit must be greater than zero");
        
        ConcurrentMessageLimit = limit;
    }
}

/// <summary>
/// Represents a consumer registration
/// </summary>
internal class ConsumerRegistration
{
    public ConsumerRegistration(Type consumerType, Func<IServiceProvider, object>? factory, bool isMiddleware = false)
    {
        ConsumerType = consumerType;
        Factory = factory;
        IsMiddleware = isMiddleware;
    }

    public Type ConsumerType { get; }
    public Func<IServiceProvider, object>? Factory { get; }
    public bool IsMiddleware { get; }
}

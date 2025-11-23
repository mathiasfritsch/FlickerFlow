using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Middleware;

namespace FlickerFlow.Core.Configuration;

/// <summary>
/// Implementation of receive endpoint configurator
/// </summary>
internal class ReceiveEndpointConfigurator : IReceiveEndpointConfigurator
{
    private readonly ReceiveEndpointConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public ReceiveEndpointConfigurator(ReceiveEndpointConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public void Consumer<T>() where T : class, IConsumer
    {
        var consumerType = typeof(T);
        
        // Check if already registered
        if (_configuration.Consumers.Any(c => c.ConsumerType == consumerType))
        {
            return;
        }

        _configuration.Consumers.Add(new ConsumerRegistration(consumerType));
    }

    public void Consumer<T>(Func<IServiceProvider, T> factory) where T : class, IConsumer
    {
        var consumerType = typeof(T);
        
        // Check if already registered
        if (_configuration.Consumers.Any(c => c.ConsumerType == consumerType))
        {
            return;
        }

        _configuration.Consumers.Add(new ConsumerRegistration(consumerType, sp => factory(sp)!));
    }

    public void UseMiddleware<T>() where T : IConsumeMiddleware
    {
        var middlewareType = typeof(T);
        _configuration.Consumers.Add(new ConsumerRegistration(middlewareType, isMiddleware: true));
    }

    public void UseMiddleware(IConsumeMiddleware middleware)
    {
        _configuration.Middlewares.Add(middleware);
    }

    public void ConfigurePrefetchCount(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentException("Prefetch count must be greater than 0", nameof(count));
        }

        _configuration.PrefetchCount = count;
    }

    public void ConfigureConcurrentMessageLimit(int limit)
    {
        if (limit <= 0)
        {
            throw new ArgumentException("Concurrent message limit must be greater than 0", nameof(limit));
        }

        _configuration.ConcurrentMessageLimit = limit;
    }
}

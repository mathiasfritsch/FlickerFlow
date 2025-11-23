using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Middleware;

namespace FlickerFlow.Core.Configuration;

/// <summary>
/// Configuration for a receive endpoint
/// </summary>
internal class ReceiveEndpointConfiguration
{
    public List<ConsumerRegistration> Consumers { get; } = new();
    public List<IConsumeMiddleware> Middlewares { get; } = new();
    public int PrefetchCount { get; set; } = 16;
    public int ConcurrentMessageLimit { get; set; } = 10;
}

/// <summary>
/// Consumer registration information
/// </summary>
internal class ConsumerRegistration
{
    public Type ConsumerType { get; }
    public Func<IServiceProvider, object>? Factory { get; }
    public bool IsMiddleware { get; }

    public ConsumerRegistration(Type consumerType, Func<IServiceProvider, object>? factory = null, bool isMiddleware = false)
    {
        ConsumerType = consumerType;
        Factory = factory;
        IsMiddleware = isMiddleware;
    }
}

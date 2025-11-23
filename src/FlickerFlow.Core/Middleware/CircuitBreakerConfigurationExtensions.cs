using FlickerFlow.Abstractions;

namespace FlickerFlow.Core.Middleware;

/// <summary>
/// Configuration extensions for circuit breaker middleware
/// </summary>
public static class CircuitBreakerConfigurationExtensions
{
    /// <summary>
    /// Configure circuit breaker for the receive endpoint
    /// </summary>
    public static void UseCircuitBreaker(
        this IReceiveEndpointConfigurator configurator,
        Action<ICircuitBreakerConfigurator> configure)
    {
        var circuitBreakerConfigurator = new CircuitBreakerConfigurator();
        configure(circuitBreakerConfigurator);
        
        // The middleware will be registered through the configurator
        // Implementation depends on the concrete configurator implementation
    }
}

/// <summary>
/// Configurator for circuit breaker
/// </summary>
public interface ICircuitBreakerConfigurator
{
    /// <summary>
    /// Set the failure threshold to open the circuit
    /// </summary>
    ICircuitBreakerConfigurator SetFailureThreshold(int threshold);

    /// <summary>
    /// Set the time window for tracking failures
    /// </summary>
    ICircuitBreakerConfigurator SetTrackingPeriod(TimeSpan period);

    /// <summary>
    /// Set the timeout before attempting to close the circuit
    /// </summary>
    ICircuitBreakerConfigurator SetResetTimeout(TimeSpan timeout);

    /// <summary>
    /// Build the circuit breaker configuration
    /// </summary>
    CircuitBreakerConfiguration Build();
}

/// <summary>
/// Default implementation of circuit breaker configurator
/// </summary>
internal class CircuitBreakerConfigurator : ICircuitBreakerConfigurator
{
    private readonly CircuitBreakerConfiguration _configuration = new();

    public ICircuitBreakerConfigurator SetFailureThreshold(int threshold)
    {
        _configuration.FailureThreshold = threshold;
        return this;
    }

    public ICircuitBreakerConfigurator SetTrackingPeriod(TimeSpan period)
    {
        _configuration.TrackingPeriod = period;
        return this;
    }

    public ICircuitBreakerConfigurator SetResetTimeout(TimeSpan timeout)
    {
        _configuration.ResetTimeout = timeout;
        return this;
    }

    public CircuitBreakerConfiguration Build()
    {
        _configuration.Validate();
        return _configuration;
    }
}

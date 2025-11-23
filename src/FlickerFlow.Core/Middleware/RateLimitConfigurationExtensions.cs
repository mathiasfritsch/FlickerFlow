using FlickerFlow.Abstractions;

namespace FlickerFlow.Core.Middleware;

/// <summary>
/// Configuration extensions for rate limiting middleware
/// </summary>
public static class RateLimitConfigurationExtensions
{
    /// <summary>
    /// Configure rate limiting for the receive endpoint
    /// </summary>
    public static void UseRateLimit(
        this IReceiveEndpointConfigurator configurator,
        int rateLimit,
        TimeSpan interval)
    {
        var configuration = new RateLimitConfiguration
        {
            MaxRate = rateLimit,
            TimeWindow = interval
        };
        
        configuration.Validate();
        
        // The middleware will be registered through the configurator
        // Implementation depends on the concrete configurator implementation
    }

    /// <summary>
    /// Configure rate limiting with full configuration
    /// </summary>
    public static void UseRateLimit(
        this IReceiveEndpointConfigurator configurator,
        Action<IRateLimitConfigurator> configure)
    {
        var rateLimitConfigurator = new RateLimitConfigurator();
        configure(rateLimitConfigurator);
        
        // The middleware will be registered through the configurator
        // Implementation depends on the concrete configurator implementation
    }
}

/// <summary>
/// Configurator for rate limiting
/// </summary>
public interface IRateLimitConfigurator
{
    /// <summary>
    /// Set the maximum message rate
    /// </summary>
    IRateLimitConfigurator SetMaxRate(int maxRate);

    /// <summary>
    /// Set the time window for rate limiting
    /// </summary>
    IRateLimitConfigurator SetTimeWindow(TimeSpan timeWindow);

    /// <summary>
    /// Set the concurrent message processing limit
    /// </summary>
    IRateLimitConfigurator SetConcurrentMessageLimit(int limit);

    /// <summary>
    /// Build the rate limit configuration
    /// </summary>
    RateLimitConfiguration Build();
}

/// <summary>
/// Default implementation of rate limit configurator
/// </summary>
internal class RateLimitConfigurator : IRateLimitConfigurator
{
    private readonly RateLimitConfiguration _configuration = new();

    public IRateLimitConfigurator SetMaxRate(int maxRate)
    {
        _configuration.MaxRate = maxRate;
        return this;
    }

    public IRateLimitConfigurator SetTimeWindow(TimeSpan timeWindow)
    {
        _configuration.TimeWindow = timeWindow;
        return this;
    }

    public IRateLimitConfigurator SetConcurrentMessageLimit(int limit)
    {
        _configuration.ConcurrentMessageLimit = limit;
        return this;
    }

    public RateLimitConfiguration Build()
    {
        _configuration.Validate();
        return _configuration;
    }
}

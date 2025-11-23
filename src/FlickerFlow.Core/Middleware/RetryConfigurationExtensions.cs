using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Middleware;

namespace FlickerFlow.Core.Middleware;

/// <summary>
/// Configuration extensions for retry middleware
/// </summary>
public static class RetryConfigurationExtensions
{
    /// <summary>
    /// Configure retry policy for the receive endpoint
    /// </summary>
    public static void UseRetry(
        this IReceiveEndpointConfigurator configurator,
        Action<IRetryConfigurator> configure)
    {
        var retryConfigurator = new RetryConfigurator();
        configure(retryConfigurator);
        
        // The middleware will be registered through the configurator
        // Implementation depends on the concrete configurator implementation
    }
}

/// <summary>
/// Configurator for retry policy
/// </summary>
public interface IRetryConfigurator
{
    /// <summary>
    /// Set the maximum number of retry attempts
    /// </summary>
    IRetryConfigurator SetMaxAttempts(int maxAttempts);

    /// <summary>
    /// Set the initial delay for exponential backoff
    /// </summary>
    IRetryConfigurator SetInitialDelay(TimeSpan delay);

    /// <summary>
    /// Set the maximum delay for exponential backoff
    /// </summary>
    IRetryConfigurator SetMaxDelay(TimeSpan delay);

    /// <summary>
    /// Set a custom function to determine if an exception is transient
    /// </summary>
    IRetryConfigurator SetTransientErrorDetector(Func<Exception, bool> isTransient);

    /// <summary>
    /// Build the retry policy
    /// </summary>
    IRetryPolicy Build();
}

/// <summary>
/// Default implementation of retry configurator
/// </summary>
internal class RetryConfigurator : IRetryConfigurator
{
    private int _maxAttempts = 3;
    private TimeSpan _initialDelay = TimeSpan.FromMilliseconds(100);
    private TimeSpan _maxDelay = TimeSpan.FromSeconds(30);
    private Func<Exception, bool>? _isTransient;

    public IRetryConfigurator SetMaxAttempts(int maxAttempts)
    {
        _maxAttempts = maxAttempts;
        return this;
    }

    public IRetryConfigurator SetInitialDelay(TimeSpan delay)
    {
        _initialDelay = delay;
        return this;
    }

    public IRetryConfigurator SetMaxDelay(TimeSpan delay)
    {
        _maxDelay = delay;
        return this;
    }

    public IRetryConfigurator SetTransientErrorDetector(Func<Exception, bool> isTransient)
    {
        _isTransient = isTransient;
        return this;
    }

    public IRetryPolicy Build()
    {
        return new ExponentialRetryPolicy(_maxAttempts, _initialDelay, _maxDelay);
    }

    internal Func<Exception, bool>? GetTransientErrorDetector() => _isTransient;
}

namespace FlickerFlow.Core.Middleware;

/// <summary>
/// Configuration for circuit breaker middleware
/// </summary>
public class CircuitBreakerConfiguration
{
    /// <summary>
    /// Number of failures required to open the circuit
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Time window for tracking failures
    /// </summary>
    public TimeSpan TrackingPeriod { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Time to wait before attempting to close the circuit
    /// </summary>
    public TimeSpan ResetTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Validate the configuration
    /// </summary>
    public void Validate()
    {
        if (FailureThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(FailureThreshold), "Failure threshold must be positive");
        
        if (TrackingPeriod <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(TrackingPeriod), "Tracking period must be positive");
        
        if (ResetTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ResetTimeout), "Reset timeout must be positive");
    }
}

namespace FlickerFlow.Core.Middleware;

/// <summary>
/// Configuration for rate limiting middleware
/// </summary>
public class RateLimitConfiguration
{
    /// <summary>
    /// Maximum number of messages to process within the time window
    /// </summary>
    public int MaxRate { get; set; } = 100;

    /// <summary>
    /// Time window for rate limiting
    /// </summary>
    public TimeSpan TimeWindow { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum number of messages to process concurrently
    /// </summary>
    public int ConcurrentMessageLimit { get; set; } = 10;

    /// <summary>
    /// Validate the configuration
    /// </summary>
    public void Validate()
    {
        if (MaxRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxRate), "Max rate must be positive");
        
        if (TimeWindow <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(TimeWindow), "Time window must be positive");
        
        if (ConcurrentMessageLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(ConcurrentMessageLimit), "Concurrent message limit must be positive");
    }
}

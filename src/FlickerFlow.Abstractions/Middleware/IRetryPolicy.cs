namespace FlickerFlow.Abstractions.Middleware;

/// <summary>
/// Policy for retrying failed message processing
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    int MaxAttempts { get; }

    /// <summary>
    /// Get the delay before the next retry attempt
    /// </summary>
    /// <param name="attemptNumber">The current attempt number (1-based)</param>
    TimeSpan GetDelay(int attemptNumber);
}

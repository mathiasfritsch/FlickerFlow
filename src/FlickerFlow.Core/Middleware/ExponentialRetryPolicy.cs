using FlickerFlow.Abstractions.Middleware;

namespace FlickerFlow.Core.Middleware;

/// <summary>
/// Retry policy with exponential backoff
/// </summary>
public class ExponentialRetryPolicy : IRetryPolicy
{
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _maxDelay;

    public ExponentialRetryPolicy(int maxAttempts, TimeSpan initialDelay, TimeSpan maxDelay)
    {
        if (maxAttempts < 0)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be non-negative");
        if (initialDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(initialDelay), "Initial delay must be non-negative");
        if (maxDelay < initialDelay)
            throw new ArgumentOutOfRangeException(nameof(maxDelay), "Max delay must be greater than or equal to initial delay");

        MaxAttempts = maxAttempts;
        _initialDelay = initialDelay;
        _maxDelay = maxDelay;
    }

    public int MaxAttempts { get; }

    public TimeSpan GetDelay(int attemptNumber)
    {
        if (attemptNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be positive");

        // Calculate exponential delay: initialDelay * 2^(attemptNumber - 1)
        var delay = TimeSpan.FromMilliseconds(
            _initialDelay.TotalMilliseconds * Math.Pow(2, attemptNumber - 1));

        // Cap at max delay
        return delay > _maxDelay ? _maxDelay : delay;
    }
}

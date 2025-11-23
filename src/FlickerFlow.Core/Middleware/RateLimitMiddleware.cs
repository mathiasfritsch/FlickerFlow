using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Middleware;
using Microsoft.Extensions.Logging;

namespace FlickerFlow.Core.Middleware;

/// <summary>
/// Middleware that limits the rate of message processing
/// </summary>
public class RateLimitMiddleware : IConsumeMiddleware
{
    private readonly RateLimitConfiguration _configuration;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly TokenBucket _rateLimiter;

    public RateLimitMiddleware(
        RateLimitConfiguration configuration,
        ILogger<RateLimitMiddleware> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _concurrencyLimiter = new SemaphoreSlim(
            _configuration.ConcurrentMessageLimit,
            _configuration.ConcurrentMessageLimit);
        
        _rateLimiter = new TokenBucket(
            _configuration.MaxRate,
            _configuration.TimeWindow);
    }

    public async Task Invoke(ConsumeContext context, Func<Task> next)
    {
        // Wait for rate limit token
        var rateLimitWait = await _rateLimiter.WaitForTokenAsync(context.CancellationToken);
        if (rateLimitWait > TimeSpan.Zero)
        {
            _logger.LogDebug(
                "Rate limit reached. Message {MessageId} delayed by {Delay}ms",
                context.MessageId, rateLimitWait.TotalMilliseconds);
        }

        // Wait for concurrency slot
        await _concurrencyLimiter.WaitAsync(context.CancellationToken);
        
        try
        {
            await next();
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }
}

/// <summary>
/// Token bucket rate limiter implementation
/// </summary>
internal class TokenBucket
{
    private readonly int _maxTokens;
    private readonly TimeSpan _refillInterval;
    private readonly object _lock = new();
    private double _tokens;
    private DateTime _lastRefill;

    public TokenBucket(int maxRate, TimeSpan timeWindow)
    {
        _maxTokens = maxRate;
        _tokens = maxRate;
        _refillInterval = timeWindow;
        _lastRefill = DateTime.UtcNow;
    }

    public async Task<TimeSpan> WaitForTokenAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var delay = TryConsumeToken();
            if (delay == TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            // Wait and try again
            await Task.Delay(delay, cancellationToken);
        }
    }

    private TimeSpan TryConsumeToken()
    {
        lock (_lock)
        {
            RefillTokens();

            if (_tokens >= 1.0)
            {
                _tokens -= 1.0;
                return TimeSpan.Zero;
            }

            // Calculate how long to wait for next token
            var tokensNeeded = 1.0 - _tokens;
            var refillRate = _maxTokens / _refillInterval.TotalMilliseconds;
            var waitTime = TimeSpan.FromMilliseconds(tokensNeeded / refillRate);
            
            return waitTime;
        }
    }

    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var timeSinceLastRefill = now - _lastRefill;

        if (timeSinceLastRefill > TimeSpan.Zero)
        {
            var tokensToAdd = (_maxTokens / _refillInterval.TotalMilliseconds) * timeSinceLastRefill.TotalMilliseconds;
            _tokens = Math.Min(_maxTokens, _tokens + tokensToAdd);
            _lastRefill = now;
        }
    }
}

using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Middleware;
using Microsoft.Extensions.Logging;

namespace FlickerFlow.Core.Middleware;

/// <summary>
/// Middleware that retries failed message processing with configurable policy
/// </summary>
public class RetryMiddleware : IConsumeMiddleware
{
    private readonly IRetryPolicy _retryPolicy;
    private readonly ILogger<RetryMiddleware> _logger;
    private readonly Func<Exception, bool>? _isTransient;

    public RetryMiddleware(
        IRetryPolicy retryPolicy,
        ILogger<RetryMiddleware> logger,
        Func<Exception, bool>? isTransient = null)
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isTransient = isTransient ?? DefaultIsTransient;
    }

    public async Task Invoke(ConsumeContext context, Func<Task> next)
    {
        var attemptNumber = 0;
        Exception? lastException = null;

        while (attemptNumber <= _retryPolicy.MaxAttempts)
        {
            attemptNumber++;

            try
            {
                await next();
                return; // Success
            }
            catch (Exception ex)
            {
                lastException = ex;

                // Check if this is a transient error that should be retried
                if (_isTransient != null && !_isTransient(ex))
                {
                    _logger.LogError(ex,
                        "Non-transient error occurred processing message {MessageId}. Not retrying.",
                        context.MessageId);
                    throw;
                }

                // Check if we've exhausted retries
                if (attemptNumber > _retryPolicy.MaxAttempts)
                {
                    _logger.LogError(ex,
                        "Message {MessageId} failed after {Attempts} attempts. Moving to dead letter queue.",
                        context.MessageId, attemptNumber);
                    
                    // TODO: Route to dead letter queue
                    throw new RetryExhaustedException(
                        $"Message {context.MessageId} failed after {attemptNumber} attempts", ex);
                }

                // Calculate delay and wait before retry
                var delay = _retryPolicy.GetDelay(attemptNumber);
                _logger.LogWarning(ex,
                    "Transient error processing message {MessageId}. Retry attempt {Attempt}/{MaxAttempts} after {Delay}ms",
                    context.MessageId, attemptNumber, _retryPolicy.MaxAttempts, delay.TotalMilliseconds);

                await Task.Delay(delay, context.CancellationToken);
            }
        }

        // Should not reach here, but throw last exception if we do
        throw lastException ?? new InvalidOperationException("Retry loop completed without success or exception");
    }

    private static bool DefaultIsTransient(Exception ex)
    {
        // Default transient error detection
        // Can be extended based on specific exception types
        return ex is not ArgumentException
            && ex is not ArgumentNullException
            && ex is not InvalidOperationException
            && ex is not NotSupportedException;
    }
}

/// <summary>
/// Exception thrown when retry attempts are exhausted
/// </summary>
public class RetryExhaustedException : Exception
{
    public RetryExhaustedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

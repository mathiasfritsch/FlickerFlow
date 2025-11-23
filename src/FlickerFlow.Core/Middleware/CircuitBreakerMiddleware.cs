using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Middleware;
using Microsoft.Extensions.Logging;

namespace FlickerFlow.Core.Middleware;

/// <summary>
/// Middleware that implements circuit breaker pattern to prevent cascading failures
/// </summary>
public class CircuitBreakerMiddleware : IConsumeMiddleware
{
    private readonly CircuitBreakerConfiguration _configuration;
    private readonly ILogger<CircuitBreakerMiddleware> _logger;
    private readonly object _lock = new();
    
    private CircuitState _state = CircuitState.Closed;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private DateTime _circuitOpenedTime;

    public CircuitBreakerMiddleware(
        CircuitBreakerConfiguration configuration,
        ILogger<CircuitBreakerMiddleware> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    public async Task Invoke(ConsumeContext context, Func<Task> next)
    {
        // Check circuit state
        var currentState = GetCurrentState();

        if (currentState == CircuitState.Open)
        {
            _logger.LogWarning(
                "Circuit breaker is OPEN. Rejecting message {MessageId}",
                context.MessageId);
            throw new CircuitBreakerOpenException("Circuit breaker is open");
        }

        try
        {
            await next();
            
            // Success - record it
            OnSuccess();
        }
        catch (Exception ex)
        {
            // Failure - record it and potentially open circuit
            OnFailure(ex, context.MessageId);
            throw;
        }
    }

    private CircuitState GetCurrentState()
    {
        lock (_lock)
        {
            // If circuit is open, check if we should transition to half-open
            if (_state == CircuitState.Open)
            {
                var timeSinceOpened = DateTime.UtcNow - _circuitOpenedTime;
                if (timeSinceOpened >= _configuration.ResetTimeout)
                {
                    _logger.LogInformation(
                        "Circuit breaker transitioning from OPEN to HALF-OPEN after {Timeout}",
                        _configuration.ResetTimeout);
                    _state = CircuitState.HalfOpen;
                }
            }

            return _state;
        }
    }

    private void OnSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitState.HalfOpen)
            {
                _logger.LogInformation("Circuit breaker transitioning from HALF-OPEN to CLOSED after successful message");
                _state = CircuitState.Closed;
                _failureCount = 0;
            }
            else if (_state == CircuitState.Closed)
            {
                // Reset failure tracking if we're outside the tracking period
                var timeSinceLastFailure = DateTime.UtcNow - _lastFailureTime;
                if (timeSinceLastFailure >= _configuration.TrackingPeriod)
                {
                    _failureCount = 0;
                }
            }
        }
    }

    private void OnFailure(Exception ex, Guid messageId)
    {
        lock (_lock)
        {
            _lastFailureTime = DateTime.UtcNow;

            if (_state == CircuitState.HalfOpen)
            {
                // Failure in half-open state immediately opens the circuit
                _logger.LogWarning(ex,
                    "Circuit breaker transitioning from HALF-OPEN to OPEN after failure processing message {MessageId}",
                    messageId);
                _state = CircuitState.Open;
                _circuitOpenedTime = DateTime.UtcNow;
                _failureCount = 0;
            }
            else if (_state == CircuitState.Closed)
            {
                // Increment failure count
                _failureCount++;

                _logger.LogWarning(ex,
                    "Circuit breaker recorded failure {FailureCount}/{Threshold} for message {MessageId}",
                    _failureCount, _configuration.FailureThreshold, messageId);

                // Check if we should open the circuit
                if (_failureCount >= _configuration.FailureThreshold)
                {
                    _logger.LogError(
                        "Circuit breaker transitioning from CLOSED to OPEN after {FailureCount} failures",
                        _failureCount);
                    _state = CircuitState.Open;
                    _circuitOpenedTime = DateTime.UtcNow;
                    _failureCount = 0;
                }
            }
        }
    }
}

/// <summary>
/// Circuit breaker states
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// Circuit is closed, messages flow normally
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open, messages are rejected
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open, testing if service has recovered
    /// </summary>
    HalfOpen
}

/// <summary>
/// Exception thrown when circuit breaker is open
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message)
    {
    }
}

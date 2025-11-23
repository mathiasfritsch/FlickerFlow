using System.Collections.Concurrent;
using FlickerFlow.Abstractions;
using FlickerFlow.Core.Endpoints;
using Microsoft.Extensions.Logging;

namespace FlickerFlow.Core;

/// <summary>
/// Central bus implementation for all messaging operations
/// </summary>
public class Bus : IBus
{
    private readonly ITransport _transport;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly ILogger<Bus> _logger;
    private readonly ConcurrentBag<IReceiveEndpoint> _receiveEndpoints = new();
    private readonly TimeSpan _shutdownTimeout;
    
    private bool _isStarted;
    private bool _isStopping;

    public Bus(
        ITransport transport,
        ILogger<Bus> logger,
        TimeSpan? shutdownTimeout = null)
    {
        _transport = transport;
        _logger = logger;
        _shutdownTimeout = shutdownTimeout ?? TimeSpan.FromSeconds(30);
        
        _publishEndpoint = new PublishEndpoint(transport);
        _sendEndpointProvider = new SendEndpointProvider(transport);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted)
        {
            _logger.LogWarning("Bus is already started");
            return;
        }

        _logger.LogInformation("Starting FlickerFlow bus");

        try
        {
            // Start the transport
            await _transport.StartAsync(cancellationToken);

            // Start all receive endpoints
            foreach (var endpoint in _receiveEndpoints)
            {
                await endpoint.StartAsync(cancellationToken);
            }

            _isStarted = true;
            _logger.LogInformation("FlickerFlow bus started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start FlickerFlow bus");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted || _isStopping)
        {
            _logger.LogWarning("Bus is not started or already stopping");
            return;
        }

        _isStopping = true;
        _logger.LogInformation("Stopping FlickerFlow bus with timeout {Timeout}", _shutdownTimeout);

        try
        {
            using var timeoutCts = new CancellationTokenSource(_shutdownTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                // Stop all receive endpoints (this waits for in-flight messages)
                var stopTasks = _receiveEndpoints.Select(e => e.StopAsync(linkedCts.Token)).ToList();
                await Task.WhenAll(stopTasks);

                // Stop the transport
                await _transport.StopAsync(linkedCts.Token);

                _logger.LogInformation("FlickerFlow bus stopped gracefully");
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                _logger.LogWarning("Shutdown timeout expired, forcing stop");
                
                // Force stop if timeout expires
                var forceStopTasks = _receiveEndpoints.Select(e => e.StopAsync(CancellationToken.None)).ToList();
                await Task.WhenAll(forceStopTasks);
                await _transport.StopAsync(CancellationToken.None);
                
                _logger.LogInformation("FlickerFlow bus stopped forcefully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping FlickerFlow bus");
            throw;
        }
        finally
        {
            _isStarted = false;
            _isStopping = false;
        }
    }

    public ConnectHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configure)
    {
        if (_isStarted)
        {
            throw new InvalidOperationException("Cannot connect receive endpoints after the bus has started");
        }

        _logger.LogInformation("Connecting receive endpoint {QueueName}", queueName);

        // Create the receive endpoint through the transport
        var endpoint = _transport.CreateReceiveEndpoint(queueName, configure);
        _receiveEndpoints.Add(endpoint);

        return new ConnectHandleImpl(endpoint, e =>
        {
            _logger.LogInformation("Disconnecting receive endpoint {EndpointName}", e.EndpointName);
            // Note: Cannot remove from ConcurrentBag, but endpoint is stopped
        });
    }

    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        if (!_isStarted)
        {
            throw new InvalidOperationException("Bus must be started before publishing messages");
        }

        return _publishEndpoint.Publish(message, cancellationToken);
    }

    public Task Publish<T>(T message, Action<PublishContext<T>> configure, CancellationToken cancellationToken = default) where T : class
    {
        if (!_isStarted)
        {
            throw new InvalidOperationException("Bus must be started before publishing messages");
        }

        return _publishEndpoint.Publish(message, configure, cancellationToken);
    }

    public Task<ISendEndpoint> GetSendEndpoint(Uri address)
    {
        if (!_isStarted)
        {
            throw new InvalidOperationException("Bus must be started before getting send endpoints");
        }

        return _sendEndpointProvider.GetSendEndpoint(address);
    }
}

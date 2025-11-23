using System.Collections.Concurrent;
using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Middleware;
using FlickerFlow.Core.Context;
using FlickerFlow.Core.Middleware;
using FlickerFlow.Core.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlickerFlow.Core.Endpoints;

/// <summary>
/// Manages message consumption and consumer invocation
/// </summary>
internal class ReceiveEndpoint : IReceiveEndpoint
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly ReceiveEndpointConfiguration _configuration;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<ReceiveEndpoint> _logger;
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly CancellationTokenSource _stoppingCts = new();
    private readonly ConcurrentDictionary<Type, MiddlewarePipeline<ConsumeContext>> _pipelines = new();
    
    private bool _isStarted;

    public ReceiveEndpoint(
        string endpointName,
        IServiceProvider serviceProvider,
        IPublishEndpoint publishEndpoint,
        ISendEndpointProvider sendEndpointProvider,
        ReceiveEndpointConfiguration configuration,
        IMessageSerializer serializer,
        ILogger<ReceiveEndpoint> logger)
    {
        EndpointName = endpointName;
        _serviceProvider = serviceProvider;
        _publishEndpoint = publishEndpoint;
        _sendEndpointProvider = sendEndpointProvider;
        _configuration = configuration;
        _serializer = serializer;
        _logger = logger;
        _concurrencyLimiter = new SemaphoreSlim(configuration.ConcurrentMessageLimit);
    }

    public string EndpointName { get; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _isStarted = true;
        _logger.LogInformation("Receive endpoint {EndpointName} started", EndpointName);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isStarted = false;
        _stoppingCts.Cancel();
        
        // Wait for in-flight messages to complete
        _logger.LogInformation("Stopping receive endpoint {EndpointName}, waiting for in-flight messages", EndpointName);
        
        // Wait until all semaphore slots are available (meaning all messages are processed)
        for (int i = 0; i < _configuration.ConcurrentMessageLimit; i++)
        {
            await _concurrencyLimiter.WaitAsync(cancellationToken);
        }
        
        // Release all slots
        _concurrencyLimiter.Release(_configuration.ConcurrentMessageLimit);
        
        _logger.LogInformation("Receive endpoint {EndpointName} stopped", EndpointName);
    }

    /// <summary>
    /// Process an incoming message envelope
    /// </summary>
    public async Task ProcessMessage(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (!_isStarted)
        {
            _logger.LogWarning("Received message on stopped endpoint {EndpointName}", EndpointName);
            return;
        }

        await _concurrencyLimiter.WaitAsync(cancellationToken);
        
        try
        {
            // Deserialize the message
            var messageType = Type.GetType(envelope.MessageType);
            if (messageType == null)
            {
                _logger.LogError("Cannot resolve message type {MessageType}", envelope.MessageType);
                return;
            }

            var message = _serializer.Deserialize(envelope.Payload, messageType);
            if (message == null)
            {
                _logger.LogError("Failed to deserialize message of type {MessageType}", envelope.MessageType);
                return;
            }

            // Find consumers for this message type
            var consumerType = typeof(IConsumer<>).MakeGenericType(messageType);
            var consumers = _configuration.Consumers
                .Where(c => !c.IsMiddleware && consumerType.IsAssignableFrom(c.ConsumerType))
                .ToList();

            if (!consumers.Any())
            {
                _logger.LogWarning("No consumers registered for message type {MessageType} on endpoint {EndpointName}", 
                    messageType.Name, EndpointName);
                return;
            }

            // Process message with each consumer
            foreach (var consumerReg in consumers)
            {
                await InvokeConsumer(consumerReg, message, messageType, envelope, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId} on endpoint {EndpointName}", 
                envelope.MessageId, EndpointName);
            throw;
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    private async Task InvokeConsumer(
        ConsumerRegistration consumerReg,
        object message,
        Type messageType,
        MessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create consume context
            var contextType = typeof(ConsumeContextImpl<>).MakeGenericType(messageType);
            var context = Activator.CreateInstance(
                contextType,
                message,
                envelope,
                _publishEndpoint,
                _sendEndpointProvider,
                cancellationToken) as ConsumeContext;

            if (context == null)
            {
                _logger.LogError("Failed to create consume context for message type {MessageType}", messageType.Name);
                return;
            }

            // Get or create pipeline for this message type
            var pipeline = _pipelines.GetOrAdd(messageType, _ => CreatePipeline(messageType));

            // Execute pipeline
            await pipeline.Execute(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Consumer {ConsumerType} failed to process message {MessageId}", 
                consumerReg.ConsumerType.Name, envelope.MessageId);
            
            // Apply error handling policy
            envelope.RetryCount++;
            throw;
        }
    }

    private MiddlewarePipeline<ConsumeContext> CreatePipeline(Type messageType)
    {
        // Resolve middleware instances
        var middlewares = new List<IConsumeMiddleware>(_configuration.Middlewares);
        
        // Add middleware from registrations
        foreach (var reg in _configuration.Consumers.Where(c => c.IsMiddleware))
        {
            var middleware = (reg.Factory != null 
                ? reg.Factory(_serviceProvider) 
                : _serviceProvider.GetRequiredService(reg.ConsumerType)) as IConsumeMiddleware;
            
            if (middleware != null)
            {
                middlewares.Add(middleware);
            }
        }

        // Final handler: invoke the actual consumer
        async Task FinalHandler(ConsumeContext context)
        {
            var consumerType = typeof(IConsumer<>).MakeGenericType(messageType);
            var consumers = _configuration.Consumers
                .Where(c => !c.IsMiddleware && consumerType.IsAssignableFrom(c.ConsumerType))
                .ToList();

            foreach (var consumerReg in consumers)
            {
                var consumer = consumerReg.Factory != null
                    ? consumerReg.Factory(_serviceProvider)
                    : _serviceProvider.GetRequiredService(consumerReg.ConsumerType);

                // Invoke Consume method
                var consumeMethod = consumerType.GetMethod("Consume");
                if (consumeMethod != null)
                {
                    var task = consumeMethod.Invoke(consumer, new object[] { context }) as Task;
                    if (task != null)
                    {
                        await task;
                    }
                }
            }
        }

        return new MiddlewarePipeline<ConsumeContext>(middlewares, FinalHandler);
    }
}

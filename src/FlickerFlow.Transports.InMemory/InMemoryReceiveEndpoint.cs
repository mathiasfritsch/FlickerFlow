using FlickerFlow.Abstractions;
using FlickerFlow.Transports.InMemory.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlickerFlow.Transports.InMemory;

/// <summary>
/// In-memory receive endpoint that processes messages from a queue
/// </summary>
internal class InMemoryReceiveEndpoint : IReceiveEndpoint
{
    private readonly string _endpointName;
    private readonly InMemoryQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITransport _transport;
    private readonly IMessageSerializer _serializer;
    private readonly TestHarness _testHarness;
    private readonly Action<IReceiveEndpointConfigurator> _configure;
    private readonly ILogger<InMemoryReceiveEndpoint> _logger;
    private readonly CancellationTokenSource _stoppingCts = new();
    private readonly List<Type> _consumerTypes = new();
    private readonly SemaphoreSlim _concurrencyLimiter;
    
    private Task? _processingTask;
    private bool _isStarted;
    private int _concurrentMessageLimit = 10;

    public InMemoryReceiveEndpoint(
        string endpointName,
        InMemoryQueue queue,
        IServiceProvider serviceProvider,
        ITransport transport,
        IMessageSerializer serializer,
        TestHarness testHarness,
        Action<IReceiveEndpointConfigurator> configure)
    {
        _endpointName = endpointName;
        _queue = queue;
        _serviceProvider = serviceProvider;
        _transport = transport;
        _serializer = serializer;
        _testHarness = testHarness;
        _configure = configure;
        _logger = serviceProvider.GetRequiredService<ILogger<InMemoryReceiveEndpoint>>();
        
        // Configure the endpoint to collect consumer types
        var configurator = new InMemoryReceiveEndpointConfigurator(_consumerTypes, limit => _concurrentMessageLimit = limit);
        configure(configurator);
        
        _concurrencyLimiter = new SemaphoreSlim(_concurrentMessageLimit);
    }

    public string EndpointName => _endpointName;
    
    public IReadOnlyList<Type> GetConsumerTypes() => _consumerTypes;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted)
        {
            return Task.CompletedTask;
        }

        _isStarted = true;
        _logger.LogInformation("Starting in-memory receive endpoint {EndpointName}", _endpointName);

        // Start processing messages from the queue
        _processingTask = Task.Run(() => ProcessMessages(_stoppingCts.Token), cancellationToken);
        
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted)
        {
            return;
        }

        _isStarted = false;
        _stoppingCts.Cancel();
        
        _logger.LogInformation("Stopping in-memory receive endpoint {EndpointName}", _endpointName);

        // Wait for processing to complete
        if (_processingTask != null)
        {
            try
            {
                await _processingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
        }
        
        // Wait for in-flight messages
        for (int i = 0; i < _concurrentMessageLimit; i++)
        {
            await _concurrencyLimiter.WaitAsync(cancellationToken);
        }
        _concurrencyLimiter.Release(_concurrentMessageLimit);
    }

    private async Task ProcessMessages(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var envelope in _queue.Reader.ReadAllAsync(cancellationToken))
            {
                if (!_isStarted || cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await _concurrencyLimiter.WaitAsync(cancellationToken);
                
                // Process message in background to allow concurrent processing
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessMessage(envelope, cancellationToken);
                    }
                    finally
                    {
                        _concurrencyLimiter.Release();
                    }
                }, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
    }

    private async Task ProcessMessage(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
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

            // Track consumption for test harness
            TrackConsumed(message, envelope);

            // Find and invoke consumers
            var consumerType = typeof(IConsumer<>).MakeGenericType(messageType);
            var consumers = _consumerTypes
                .Where(t => consumerType.IsAssignableFrom(t))
                .ToList();

            if (!consumers.Any())
            {
                _logger.LogWarning("No consumers registered for message type {MessageType} on endpoint {EndpointName}", 
                    messageType.Name, _endpointName);
                return;
            }

            // Create consume context
            var contextType = typeof(InMemoryConsumeContext<>).MakeGenericType(messageType);
            var context = Activator.CreateInstance(
                contextType,
                message,
                envelope,
                _transport,
                cancellationToken) as ConsumeContext;

            if (context == null)
            {
                _logger.LogError("Failed to create consume context for message type {MessageType}", messageType.Name);
                return;
            }

            // Invoke each consumer
            foreach (var consType in consumers)
            {
                using var scope = _serviceProvider.CreateScope();
                var consumer = scope.ServiceProvider.GetRequiredService(consType);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId} on endpoint {EndpointName}", 
                envelope.MessageId, _endpointName);
        }
    }

    private void TrackConsumed(object message, MessageEnvelope envelope)
    {
        var messageType = message.GetType();
        var trackMethod = typeof(TestHarness).GetMethod("TrackConsumed", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        if (trackMethod != null)
        {
            var genericMethod = trackMethod.MakeGenericMethod(messageType);
            genericMethod.Invoke(_testHarness, new[] { message, envelope.MessageId, envelope.CorrelationId, EndpointName });
        }
    }
}

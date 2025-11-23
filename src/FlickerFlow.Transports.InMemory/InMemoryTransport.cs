using System.Collections.Concurrent;
using FlickerFlow.Abstractions;
using FlickerFlow.Core.Serialization;
using FlickerFlow.Transports.InMemory.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlickerFlow.Transports.InMemory;

/// <summary>
/// In-memory transport implementation for testing and development
/// </summary>
public class InMemoryTransport : ITransport
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<InMemoryTransport> _logger;
    private readonly TestHarness _testHarness;
    private readonly ConcurrentDictionary<string, InMemoryQueue> _queues = new();
    private readonly ConcurrentDictionary<Type, List<InMemoryQueue>> _subscriptions = new();
    private readonly List<IReceiveEndpoint> _receiveEndpoints = new();
    private readonly List<object> _receiveEndpointSpecs;

    public InMemoryTransport(
        IServiceProvider serviceProvider,
        object receiveEndpointSpecs)
    {
        _serviceProvider = serviceProvider;
        _receiveEndpointSpecs = receiveEndpointSpecs as List<object> ?? new List<object>();
        _serializer = serviceProvider.GetService<IMessageSerializer>() ?? new SystemTextJsonSerializer();
        _logger = serviceProvider.GetRequiredService<ILogger<InMemoryTransport>>();
        _testHarness = new TestHarness();
    }

    /// <summary>
    /// Get the test harness for verifying message flow
    /// </summary>
    public ITestHarness TestHarness => _testHarness;

    public Task<ISendEndpoint> GetSendEndpoint(Uri address)
    {
        var queueName = address.AbsolutePath.TrimStart('/');
        var queue = _queues.GetOrAdd(queueName, name => new InMemoryQueue(name));
        
        ISendEndpoint endpoint = new InMemorySendEndpoint(queue, _serializer, _testHarness);
        return Task.FromResult(endpoint);
    }

    public Task<IPublishEndpoint> GetPublishEndpoint<T>() where T : class
    {
        IPublishEndpoint endpoint = new InMemoryPublishEndpoint<T>(
            _subscriptions,
            _serializer,
            _testHarness);
        
        return Task.FromResult(endpoint);
    }

    public IReceiveEndpoint CreateReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configure)
    {
        var queue = _queues.GetOrAdd(queueName, name => new InMemoryQueue(name));
        
        // Create the in-memory receive endpoint
        var inMemoryReceiveEndpoint = new InMemoryReceiveEndpoint(
            queueName,
            queue,
            _serviceProvider,
            this,
            _serializer,
            _testHarness,
            configure);

        _receiveEndpoints.Add(inMemoryReceiveEndpoint);

        // Register subscriptions for all consumer message types
        foreach (var consumerType in inMemoryReceiveEndpoint.GetConsumerTypes())
        {
            var consumerInterfaces = consumerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .ToList();

            foreach (var consumerInterface in consumerInterfaces)
            {
                var messageType = consumerInterface.GetGenericArguments()[0];
                var subscriptionList = _subscriptions.GetOrAdd(messageType, _ => new List<InMemoryQueue>());
                
                lock (subscriptionList)
                {
                    if (!subscriptionList.Contains(queue))
                    {
                        subscriptionList.Add(queue);
                    }
                }
            }
        }

        return inMemoryReceiveEndpoint;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("In-memory transport started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("In-memory transport stopped");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reset the transport state for testing
    /// </summary>
    public void Reset()
    {
        _queues.Clear();
        _subscriptions.Clear();
        _testHarness.Reset();
    }
}

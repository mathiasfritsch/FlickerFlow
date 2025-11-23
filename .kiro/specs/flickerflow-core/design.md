# FlickerFlow Design Document

## Overview

FlickerFlow is a distributed messaging framework for .NET that provides a unified abstraction over multiple message transport implementations. The framework enables developers to build scalable, loosely-coupled applications using message-based communication patterns without being tied to specific message broker implementations.

### Core Design Principles

1. **Transport Abstraction**: Provide a consistent API across all transport implementations (RabbitMQ, Azure Service Bus, Amazon SQS, Kafka, SQL, In-Memory)
2. **Dependency Injection First**: Leverage .NET's built-in DI container for configuration and component resolution
3. **Middleware Pipeline**: Support extensible message processing through composable middleware
4. **Type Safety**: Use generic types to provide compile-time safety for message handling
5. **Async by Default**: All operations are asynchronous to support high-throughput scenarios
6. **Observability**: Built-in support for diagnostics, tracing, and metrics

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Application Layer                       │
│  (Publishers, Senders, Consumers, Request Clients, Sagas)   │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                         IBus Interface                       │
│         (IPublishEndpoint, ISendEndpointProvider)           │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                    Middleware Pipeline                       │
│  (Retry, Circuit Breaker, Rate Limiting, Logging, etc.)     │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                   Transport Abstraction                      │
│              (ITransport, IReceiveEndpoint)                  │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                  Transport Implementations                   │
│  RabbitMQ │ Azure SB │ Amazon SQS │ Kafka │ SQL │ In-Memory │
└─────────────────────────────────────────────────────────────┘
```

### Layered Architecture

**Application Layer**: Consumer implementations, message publishers, and saga definitions
**Bus Layer**: Central coordination point for all messaging operations
**Pipeline Layer**: Middleware components for cross-cutting concerns
**Transport Layer**: Abstract interfaces for transport operations
**Implementation Layer**: Concrete transport implementations

### Design Rationale

- **Layered approach** allows clear separation of concerns and makes the system testable
- **Transport abstraction** enables switching between message brokers without code changes
- **Middleware pipeline** provides flexibility for adding behaviors without modifying core logic
- **Centralized bus** simplifies application code by providing a single entry point

## Components and Interfaces

### Core Messaging Interfaces

#### IBus
The central interface for all messaging operations.

```csharp
public interface IBus : IPublishEndpoint, ISendEndpointProvider
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    ConnectHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configure);
}
```

**Design Decision**: IBus combines publishing and sending capabilities to provide a unified API. This reduces the number of dependencies applications need to inject.

#### IPublishEndpoint
Interface for broadcasting messages to subscribers.

```csharp
public interface IPublishEndpoint
{
    Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class;
    Task Publish<T>(T message, Action<PublishContext<T>> configure, CancellationToken cancellationToken = default) where T : class;
}
```

**Design Decision**: Generic type parameter ensures compile-time type safety. The configure overload allows setting headers and metadata.

#### ISendEndpoint
Interface for point-to-point messaging.

```csharp
public interface ISendEndpoint
{
    Task Send<T>(T message, CancellationToken cancellationToken = default) where T : class;
    Task Send<T>(T message, Action<SendContext<T>> configure, CancellationToken cancellationToken = default) where T : class;
}
```

#### ISendEndpointProvider
Factory for obtaining send endpoints by URI.

```csharp
public interface ISendEndpointProvider
{
    Task<ISendEndpoint> GetSendEndpoint(Uri address);
}
```

**Design Decision**: URI-based addressing allows flexible endpoint resolution and supports different transport schemes (rabbitmq://, azuresb://, etc.).

#### IConsumer<TMessage>
Interface for message consumers.

```csharp
public interface IConsumer<in TMessage> where TMessage : class
{
    Task Consume(ConsumeContext<TMessage> context);
}
```

**Design Decision**: Generic interface with contravariant type parameter allows polymorphic message handling. Async method supports non-blocking processing.

#### ConsumeContext<TMessage>
Provides message and metadata to consumers.

```csharp
public interface ConsumeContext<out TMessage> where TMessage : class
{
    TMessage Message { get; }
    Guid MessageId { get; }
    Guid? CorrelationId { get; }
    DateTime Timestamp { get; }
    Headers Headers { get; }
    CancellationToken CancellationToken { get; }
    
    Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class;
    Task Send<T>(Uri destinationAddress, T message, CancellationToken cancellationToken = default) where T : class;
    Task RespondAsync<T>(T message) where T : class;
}
```

**Design Decision**: ConsumeContext provides both message data and operations, allowing consumers to publish events or send commands without injecting IBus.

### Transport Abstraction

#### ITransport
Base interface for all transport implementations.

```csharp
public interface ITransport
{
    Task<ISendEndpoint> GetSendEndpoint(Uri address);
    Task<IPublishEndpoint> GetPublishEndpoint<T>() where T : class;
    IReceiveEndpoint CreateReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configure);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
```

#### IReceiveEndpoint
Represents a queue or subscription that receives messages.

```csharp
public interface IReceiveEndpoint
{
    string EndpointName { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
```

**Design Decision**: Separate start/stop methods allow fine-grained control over endpoint lifecycle.

### Configuration API

#### IServiceCollection Extensions

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFlickerFlow(
        this IServiceCollection services,
        Action<IBusConfigurator> configure)
    {
        // Register core services
        // Apply configuration
        // Return services for chaining
    }
}
```

#### IBusConfigurator
Fluent API for configuring the bus.

```csharp
public interface IBusConfigurator
{
    void AddConsumer<T>() where T : class, IConsumer;
    void AddConsumers(Assembly assembly);
    
    void UsingRabbitMq(Action<IRabbitMqBusFactoryConfigurator> configure);
    void UsingAzureServiceBus(Action<IAzureServiceBusBusFactoryConfigurator> configure);
    void UsingAmazonSqs(Action<IAmazonSqsBusFactoryConfigurator> configure);
    void UsingKafka(Action<IKafkaBusFactoryConfigurator> configure);
    void UsingSql(Action<ISqlBusFactoryConfigurator> configure);
    void UsingInMemory(Action<IInMemoryBusFactoryConfigurator> configure);
}
```

#### IReceiveEndpointConfigurator
Configures individual receive endpoints.

```csharp
public interface IReceiveEndpointConfigurator
{
    void Consumer<T>() where T : class, IConsumer;
    void Consumer<T>(Func<IServiceProvider, T> factory) where T : class, IConsumer;
    
    void UseRetry(Action<IRetryConfigurator> configure);
    void UseCircuitBreaker(Action<ICircuitBreakerConfigurator> configure);
    void UseRateLimit(int rateLimit, TimeSpan interval);
    void UseMiddleware<T>() where T : IMiddleware;
    
    void ConfigurePrefetchCount(int count);
    void ConfigureConcurrentMessageLimit(int limit);
}
```

**Design Decision**: Fluent configuration API provides discoverability and type safety. Transport-specific configurators allow access to transport-specific features.

### Middleware Pipeline

#### IMiddleware
Base interface for middleware components.

```csharp
public interface IMiddleware<TContext>
{
    Task Invoke(TContext context, Func<Task> next);
}
```

#### Built-in Middleware

1. **Retry Middleware**: Implements exponential backoff retry logic
2. **Circuit Breaker Middleware**: Prevents cascading failures
3. **Rate Limiting Middleware**: Controls message processing rate
4. **Logging Middleware**: Logs message processing events
5. **Validation Middleware**: Validates messages before processing
6. **Transaction Middleware**: Manages database transactions

**Design Decision**: Middleware pattern allows composable behaviors. Each middleware can transform context, short-circuit the pipeline, or add cross-cutting concerns.

### Request/Response Pattern

#### IRequestClient<TRequest>
Client for request/response communication.

```csharp
public interface IRequestClient<TRequest> where TRequest : class
{
    Task<Response<TResponse>> GetResponse<TResponse>(TRequest request, CancellationToken cancellationToken = default, TimeSpan? timeout = null) where TResponse : class;
}
```

#### Response<T>
Wraps response messages.

```csharp
public class Response<T> where T : class
{
    public T Message { get; }
    public Guid RequestId { get; }
    public Guid ConversationId { get; }
}
```

**Design Decision**: Temporary response queues are created per request to avoid queue proliferation. Correlation IDs link requests to responses.

### Saga Pattern

#### ISaga
Base interface for saga state machines.

```csharp
public interface ISaga
{
    Guid CorrelationId { get; }
}
```

#### ISagaRepository<TSaga>
Persists saga state.

```csharp
public interface ISagaRepository<TSaga> where TSaga : class, ISaga
{
    Task<TSaga> Load(Guid correlationId);
    Task Save(TSaga saga);
    Task Delete(Guid correlationId);
}
```

#### Saga State Machine

```csharp
public abstract class SagaStateMachine<TSaga> where TSaga : class, ISaga
{
    protected State Initial { get; set; }
    protected State Final { get; set; }
    
    protected Event<TMessage> Event<TMessage>() where TMessage : class;
    
    protected void During(State state, Func<Event, Task> handler);
    protected void TransitionTo(State state);
}
```

**Design Decision**: State machine pattern provides clear saga lifecycle management. Repository abstraction allows different persistence strategies (SQL, NoSQL, in-memory).

### Message Scheduling

#### IMessageScheduler
Interface for scheduling messages.

```csharp
public interface IMessageScheduler
{
    Task<ScheduledMessage<T>> ScheduleSend<T>(Uri destinationAddress, DateTime scheduledTime, T message) where T : class;
    Task<ScheduledMessage<T>> SchedulePublish<T>(DateTime scheduledTime, T message) where T : class;
    Task CancelScheduledSend(Guid tokenId);
}
```

**Design Decision**: Scheduling is implemented using transport-specific features (RabbitMQ delayed exchange, Azure Service Bus scheduled messages) or a SQL-based scheduler for transports without native support.

## Data Models

### Message Envelope
Internal representation of messages with metadata.

```csharp
public class MessageEnvelope
{
    public Guid MessageId { get; set; }
    public Guid? CorrelationId { get; set; }
    public Guid? ConversationId { get; set; }
    public DateTime Timestamp { get; set; }
    public string MessageType { get; set; }
    public string ContentType { get; set; }
    public byte[] Body { get; set; }
    public Dictionary<string, object> Headers { get; set; }
    public string SourceAddress { get; set; }
    public string DestinationAddress { get; set; }
    public string ResponseAddress { get; set; }
    public string FaultAddress { get; set; }
    public int RetryCount { get; set; }
}
```

**Design Decision**: Envelope pattern separates message payload from metadata. This allows transport-agnostic message handling.

### Topology Configuration

```csharp
public class TopologyConfiguration
{
    public string ExchangeName { get; set; }
    public string QueueName { get; set; }
    public ExchangeType ExchangeType { get; set; }
    public bool Durable { get; set; }
    public bool AutoDelete { get; set; }
    public Dictionary<string, object> Arguments { get; set; }
}
```

### Connection Configuration

```csharp
public class ConnectionConfiguration
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string VirtualHost { get; set; }
    public TimeSpan ConnectionTimeout { get; set; }
    public int RetryCount { get; set; }
}
```

## Error Handling

### Error Handling Strategy

1. **Transient Errors**: Retry with exponential backoff
2. **Poison Messages**: Move to poison queue after max retries
3. **Deserialization Errors**: Move to poison queue immediately
4. **Consumer Exceptions**: Log and apply retry policy
5. **Transport Errors**: Reconnect and retry

### Fault Message

```csharp
public class Fault<T> where T : class
{
    public Guid FaultId { get; set; }
    public Guid? FaultedMessageId { get; set; }
    public DateTime Timestamp { get; set; }
    public ExceptionInfo[] Exceptions { get; set; }
    public string Host { get; set; }
    public T Message { get; set; }
}
```

### Dead Letter Queue Strategy

- Each receive endpoint has an associated error queue
- Messages that exceed retry limits are moved to error queue
- Error queues follow naming convention: `{queue-name}_error`
- Fault consumers can subscribe to error queues for custom handling

**Design Decision**: Separate error queues per endpoint allow targeted error handling. Fault messages include original message and exception details for debugging.

## Testing Strategy

### Unit Testing
- Test individual components in isolation
- Mock transport implementations
- Test middleware pipeline composition
- Test message serialization/deserialization

### Integration Testing
- Use in-memory transport for fast integration tests
- Test consumer registration and message routing
- Test middleware behavior with real message flow
- Test saga state transitions

### Test Harness
Provide utilities for testing message-based code:

```csharp
public class InMemoryTestHarness
{
    public Task<ConsumedMessage<T>> Consumed<T>() where T : class;
    public Task<PublishedMessage<T>> Published<T>() where T : class;
    public Task<SentMessage<T>> Sent<T>() where T : class;
    public void Reset();
}
```

**Design Decision**: In-memory transport and test harness eliminate external dependencies in tests. Test harness provides assertions for message publication and consumption.

### Performance Testing
- Measure message throughput under load
- Test concurrent consumer performance
- Measure serialization overhead
- Test memory usage with long-running processes

## Observability

### Diagnostic Events

```csharp
public static class DiagnosticEvents
{
    public const string MessagePublished = "FlickerFlow.Message.Published";
    public const string MessageSent = "FlickerFlow.Message.Sent";
    public const string MessageReceived = "FlickerFlow.Message.Received";
    public const string MessageConsumed = "FlickerFlow.Message.Consumed";
    public const string MessageFailed = "FlickerFlow.Message.Failed";
    public const string ConsumerException = "FlickerFlow.Consumer.Exception";
}
```

### Metrics

- Message throughput (messages/second)
- Message latency (time from publish to consume)
- Consumer processing time
- Error rate
- Retry count
- Circuit breaker state

### Distributed Tracing

- Integrate with .NET DiagnosticSource
- Propagate trace context in message headers
- Support W3C Trace Context standard
- OpenTelemetry integration

**Design Decision**: Built-in observability enables production monitoring without additional instrumentation. DiagnosticSource integration works with existing .NET monitoring tools.

## Serialization

### Serialization Strategy

1. **Default**: System.Text.Json for performance and .NET integration
2. **Alternative**: Newtonsoft.Json for compatibility
3. **Binary**: MessagePack for compact representation

### Message Type Resolution

- Include message type name in envelope
- Support type forwarding for versioning
- Handle polymorphic types through type discriminators

```csharp
public interface IMessageSerializer
{
    byte[] Serialize<T>(T message) where T : class;
    T Deserialize<T>(byte[] data) where T : class;
    object Deserialize(byte[] data, Type messageType);
    string ContentType { get; }
}
```

**Design Decision**: Pluggable serialization allows optimization for different scenarios. Type information in envelope enables dynamic deserialization.

## Transport-Specific Implementations

### RabbitMQ
- Use exchanges for pub/sub (fanout, topic)
- Use queues for point-to-point
- Leverage delayed message exchange for scheduling
- Use dead letter exchanges for error handling

### Azure Service Bus
- Use topics for pub/sub
- Use queues for point-to-point
- Use scheduled messages for scheduling
- Use dead letter queues for error handling

### Amazon SQS
- Use SNS topics for pub/sub
- Use SQS queues for point-to-point
- Use message timers for scheduling
- Use dead letter queues for error handling

### Kafka
- Use topics with multiple consumers for pub/sub
- Use topics with single consumer group for point-to-point
- Limited scheduling support (requires custom implementation)
- Use separate error topics for error handling

### SQL
- Use tables as queues
- Polling-based message retrieval
- Transaction support for exactly-once processing
- Scheduled messages via timestamp column

### In-Memory
- Dictionary-based message storage
- Synchronous delivery for testing
- No persistence (messages lost on restart)
- Full feature support for testing

**Design Decision**: Each transport implementation maps FlickerFlow concepts to native transport features. This provides optimal performance while maintaining API consistency.

## Graceful Shutdown

### Shutdown Process

1. Stop accepting new messages
2. Wait for in-flight messages to complete (with timeout)
3. Close transport connections
4. Dispose resources

```csharp
public class ShutdownConfiguration
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool ForceStopOnTimeout { get; set; } = true;
}
```

**Design Decision**: Graceful shutdown prevents message loss during application restarts. Configurable timeout balances clean shutdown with deployment speed.

## Security Considerations

### Transport Security
- Support TLS/SSL for all transports
- Credential management through configuration
- Support for managed identities (Azure)
- Support for IAM roles (AWS)

### Message Security
- Optional message encryption
- Message signing for integrity
- Support for custom authentication headers

**Design Decision**: Security is transport-specific but exposed through consistent configuration. Encryption and signing are optional to avoid performance overhead when not needed.

## Performance Considerations

### Optimization Strategies

1. **Connection Pooling**: Reuse transport connections
2. **Prefetch**: Batch message retrieval from broker
3. **Concurrent Processing**: Process multiple messages in parallel
4. **Async I/O**: Non-blocking operations throughout
5. **Object Pooling**: Reuse serialization buffers

### Configuration Options

```csharp
public class PerformanceConfiguration
{
    public int PrefetchCount { get; set; } = 16;
    public int ConcurrentMessageLimit { get; set; } = 10;
    public int ConnectionPoolSize { get; set; } = 5;
    public bool UseObjectPooling { get; set; } = true;
}
```

**Design Decision**: Performance tuning is exposed through configuration rather than requiring code changes. Defaults are chosen for balanced performance.

## Extensibility Points

### Custom Middleware
Implement `IMiddleware<ConsumeContext>` to add custom behaviors

### Custom Serializers
Implement `IMessageSerializer` for custom serialization

### Custom Transports
Implement `ITransport` for additional message brokers

### Custom Saga Repositories
Implement `ISagaRepository<TSaga>` for custom persistence

### Custom Topology
Override default topology creation through configurators

**Design Decision**: Well-defined extension points allow customization without forking the framework. Interface-based design enables dependency injection of custom implementations.

## Migration and Versioning

### Message Versioning
- Support multiple message versions simultaneously
- Use type forwarding for renamed types
- Graceful handling of missing properties

### API Versioning
- Follow semantic versioning
- Maintain backward compatibility within major versions
- Provide migration guides for breaking changes

**Design Decision**: Versioning strategy supports gradual rollouts and zero-downtime deployments.

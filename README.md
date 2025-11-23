# Core Messaging Architecture

FlickerFlow uses a message-based, loosely-coupled architecture with several key components:

## 1. Message Producers (Sending Messages)

There are two primary ways to send messages:

### Publish (Pub/Sub Pattern)

- Uses `IPublishEndpoint` interface
- Broadcasts messages to all subscribers
- The transport determines the actual destination (e.g., RabbitMQ exchange, Azure Service Bus topic)
- Example:
  ```csharp
  await bus.Publish<OrderSubmitted>(new { OrderId = 123 });
  ```

### Send (Point-to-Point)

- Uses `ISendEndpoint` interface
- Sends directly to a specific queue/endpoint
- You need to get the endpoint first:
  ```csharp
  var endpoint = await bus.GetSendEndpoint(uri);
  await endpoint.Send<ProcessOrder>(new { OrderId = 123 });
  ```
## 2. Message Consumers

Consumers implement the `IConsumer<TMessage>` interface:

```csharp
public interface IConsumer<in TMessage> : IConsumer
    where TMessage : class
{
    Task Consume(ConsumeContext<TMessage> context);
}
```

The `ConsumeContext` provides:

- Access to the message
- Ability to publish/send other messages
- Response capabilities for request/response patterns
- Message headers and metadata
- Serialization context
## 3. The Bus

The `IBus` interface is the central component that:

- Implements both `IPublishEndpoint` and `ISendEndpointProvider`
- Manages receive endpoints (queues)
- Provides topology configuration
- Handles connection management
## 4. Transport Layer

FlickerFlow abstracts different message brokers through transport implementations:

- **RabbitMQ** - Uses exchanges and queues
- **Azure Service Bus** - Uses topics and queues
- **Amazon SQS** - Uses SQS queues
- **Kafka** - Uses topics (as a "rider")
- **SQL Transport** - Uses database tables (PostgreSQL, SQL Server)
- **In-Memory** - For testing
## 5. Configuration Pattern

Based on the structure, configuration typically follows this pattern:

```csharp
// Register with dependency injection
services.AddFlickerFlow(x =>
{
    // Add consumers
    x.AddConsumer<OrderConsumer>();
    
    // Configure transport
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost");
        
        // Configure receive endpoints
        cfg.ReceiveEndpoint("order-queue", e =>
        {
            e.ConfigureConsumer<OrderConsumer>(context);
        });
    });
});
```
## 6. Key Features

### Middleware Pipeline

- Messages flow through a configurable pipeline
- Supports retry policies, circuit breakers, rate limiting, concurrency limits
- Filters can transform, validate, or route messages

### Sagas (State Machines)

- Long-running workflows using the Automatonymous state machine library
- Tracks state across multiple messages
- Supports compensation (undo operations)

### Request/Response

- `IRequestClient<TRequest>` for synchronous-style communication
- Handles correlation, timeouts, and fault handling

### Scheduling

- Delayed message delivery
- Recurring messages
- Integration with Quartz or Hangfire

### Serialization

- Default: System.Text.Json
- Also supports Newtonsoft.Json, MessagePack
- Automatic message type detection
## 7. Message Topology

FlickerFlow automatically creates the necessary infrastructure:

- **Publish**: Creates exchanges/topics and bindings
- **Send**: Creates queues
- **Consumers**: Automatically binds message types to endpoints
- Supports custom naming conventions (kebab-case, snake-case, etc.)

The framework handles all the complexity of message routing, serialization, error handling, and retry logic, letting you focus on business logic in your consumers.
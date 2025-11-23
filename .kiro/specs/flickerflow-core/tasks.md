# Implementation Plan

- [x] 1. Set up project structure and core abstractions





  - Create solution structure with projects: FlickerFlow.Core, FlickerFlow.Abstractions, FlickerFlow.Transports.InMemory, FlickerFlow.Transports.RabbitMq
  - Define core namespace structure and project references
  - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 6.1_


- [x] 1.1 Create core public interfaces in FlickerFlow.Abstractions

  - Implement IBus, IPublishEndpoint, ISendEndpoint, ISendEndpointProvider interfaces
  - Implement IConsumer<TMessage> interface with generic type parameter
  - Implement ConsumeContext, ConsumeContext<TMessage>, PublishContext<TMessage>, SendContext<TMessage> interfaces
  - _Requirements: 1.1, 1.3, 2.1, 2.2, 3.1, 3.2, 4.1_

- [x] 1.2 Create message envelope and headers model


  - Implement MessageEnvelope class with MessageId, CorrelationId, SentTime, MessageType, Headers, Payload properties
  - Implement Headers collection class for message metadata
  - Implement standard header constants (MessageId, CorrelationId, Timestamp, MessageType)
  - _Requirements: 1.4, 16.1, 16.2, 16.3, 16.4_

- [x] 1.3 Create transport abstraction interfaces


  - Implement ITransport interface with CreateReceiveEndpoint, GetSendEndpoint, GetPublishEndpoint, StartAsync, StopAsync methods
  - Implement IReceiveEndpoint interface for message consumption
  - Implement transport configuration base classes
  - _Requirements: 5.1, 5.2, 5.3_

- [x] 2. Implement serialization infrastructure












  - Create IMessageSerializer interface with Serialize and Deserialize methods
  - Implement System.Text.Json serializer as default
  - Implement type information extraction and embedding in message envelope
  - Support polymorphic message type deserialization
  - _Requirements: 1.4, 12.1, 12.3, 12.4, 12.5_

- [x] 3. Implement middleware pipeline infrastructure





  - Create IMiddleware<TContext> interface with Invoke method
  - Implement middleware pipeline builder and executor
  - Create middleware registration API in configuration
  - Support middleware ordering and composition
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 3.1 Implement retry middleware


  - Create IRetryPolicy interface with MaxAttempts and GetDelay methods
  - Implement ExponentialRetryPolicy with configurable initial delay and max delay
  - Implement retry middleware that applies policy on transient failures
  - Implement dead letter queue routing when retries are exhausted
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 3.2 Implement circuit breaker middleware


  - Create CircuitBreakerConfiguration with FailureThreshold, TrackingPeriod, ResetTimeout
  - Implement circuit breaker state machine (Closed, Open, Half-Open)
  - Implement failure tracking and threshold detection
  - Implement automatic circuit reset after timeout
  - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5_

- [x] 3.3 Implement rate limiting middleware


  - Create rate limiting configuration with max rate and time window
  - Implement token bucket or sliding window rate limiter
  - Implement concurrent message processing limits
  - Support per-endpoint rate limiting configuration
  - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5_

- [x] 3.4 Implement logging and tracing middleware


  - Create logging middleware that logs message processing events with correlation IDs
  - Implement DiagnosticSource integration for distributed tracing
  - Emit diagnostic events for Publish.Start, Publish.Stop, Send.Start, Send.Stop, Consume.Start, Consume.Stop, Consume.Error
  - _Requirements: 18.2, 19.1, 19.2, 19.4_

- [x] 4. Implement core bus and endpoint management





  - Implement Bus class that implements IBus interface
  - Implement PublishEndpoint class for message broadcasting
  - Implement SendEndpointProvider with endpoint caching
  - Implement SendEndpoint class for point-to-point messaging
  - _Requirements: 1.1, 1.2, 2.1, 2.3, 2.4, 4.1, 4.2_

- [x] 4.1 Implement bus lifecycle management


  - Implement StartAsync method to initialize transport and start receive endpoints
  - Implement StopAsync method with graceful shutdown
  - Implement shutdown timeout and in-flight message handling
  - Implement connection management through bus interface
  - _Requirements: 4.3, 4.4, 20.1, 20.2, 20.3, 20.4, 20.5_

- [x] 4.2 Implement receive endpoint and consumer invocation


  - Create ReceiveEndpoint class that manages message consumption
  - Implement consumer resolution from dependency injection container
  - Implement ConsumeContext creation with message and metadata
  - Invoke consumer's Consume method with proper error handling
  - Apply configured error handling policy on consumer exceptions
  - _Requirements: 3.2, 3.3, 3.4, 3.5, 6.5_

- [ ] 5. Implement dependency injection configuration
  - Create AddFlickerFlow extension method for IServiceCollection
  - Implement fluent configuration API for transport selection
  - Implement consumer registration through AddConsumer<T> method
  - Implement receive endpoint configuration with consumer bindings
  - Register IBus as singleton in service container
  - _Requirements: 4.5, 6.1, 6.2, 6.3, 6.4, 6.5_

- [ ] 6. Implement in-memory transport
  - Create InMemoryTransport class implementing ITransport
  - Implement in-memory message delivery using concurrent collections
  - Implement all core messaging patterns (publish, send, consume)
  - Support message routing and consumer invocation
  - _Requirements: 5.1, 5.2, 17.1, 17.2, 17.3_

- [ ] 6.1 Implement in-memory transport configuration
  - Create UsingInMemory configuration method
  - Implement in-memory receive endpoint configuration
  - Support state reset for testing scenarios
  - _Requirements: 17.5_

- [ ] 6.2 Implement test harness utilities
  - Create ITestHarness interface with Published, Consumed, Sent assertion methods
  - Implement TestHarness class that tracks message flow
  - Implement IConsumerTestHarness for consumer-specific assertions
  - Support timeout-based assertions for async verification
  - _Requirements: 17.4_

- [ ] 7. Implement RabbitMQ transport
  - Create RabbitMqTransport class implementing ITransport
  - Implement connection management using RabbitMQ.Client
  - Implement channel pooling for performance
  - Implement message serialization and deserialization
  - _Requirements: 5.1, 5.2, 5.3_

- [ ] 7.1 Implement RabbitMQ topology management
  - Implement automatic exchange creation based on message types
  - Implement automatic queue creation for receive endpoints
  - Implement bindings between exchanges and queues
  - Support custom naming conventions for topology elements
  - Support manual topology configuration option
  - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5_

- [ ] 7.2 Implement RabbitMQ publish and send operations
  - Implement IPublishEndpoint for RabbitMQ using topic exchanges
  - Implement ISendEndpoint for RabbitMQ using direct routing
  - Support message headers and metadata
  - Handle transport-specific delivery semantics
  - _Requirements: 1.1, 1.2, 1.5, 2.3, 2.4, 2.5, 5.5, 16.4_

- [ ] 7.3 Implement RabbitMQ receive endpoint
  - Implement message consumption using BasicConsume
  - Implement prefetch count configuration
  - Implement concurrent message processing
  - Implement message acknowledgment and rejection
  - _Requirements: 3.3, 5.5_

- [ ] 7.4 Implement RabbitMQ configuration API
  - Create UsingRabbitMq configuration method
  - Implement Host configuration for connection settings
  - Implement ReceiveEndpoint configuration with RabbitMQ-specific options
  - Support TLS/SSL configuration
  - _Requirements: 5.3, 6.3_

- [ ] 8. Implement error handling infrastructure
  - Create poison message queue handling for deserialization failures
  - Implement fault message publishing with exception details
  - Create Fault<TMessage> message type with original message and exception info
  - Implement IConsumer<Fault<TMessage>> support for fault consumers
  - Implement custom error handling strategies per endpoint
  - _Requirements: 18.1, 18.2, 18.3, 18.4, 18.5_

- [ ] 9. Implement request/response pattern
  - Create IRequestClient<TRequest> interface with GetResponse<TResponse> method
  - Implement temporary response queue creation with unique names
  - Implement request sending with reply-to address
  - Implement response correlation using message IDs
  - Implement timeout handling with RequestTimeoutException
  - Implement response queue cleanup
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

- [ ] 9.1 Implement RespondAsync in ConsumeContext
  - Add RespondAsync<T> method to ConsumeContext interface
  - Implement response sending to reply-to address from request
  - Set correlation ID to match request message ID
  - _Requirements: 9.2, 9.3_

- [ ] 10. Implement saga infrastructure
  - Create ISaga interface with CorrelationId property
  - Create ISagaStateMachine<TSaga> interface with state and event definitions
  - Implement saga state machine builder with fluent API
  - Implement saga repository abstraction for state persistence
  - Implement in-memory saga repository
  - _Requirements: 10.1, 10.2, 10.3, 10.4_

- [ ] 10.1 Implement saga message correlation and invocation
  - Implement saga instance resolution using CorrelationId
  - Implement saga state loading from repository
  - Implement state machine event triggering
  - Implement saga state persistence after processing
  - Implement compensation action support
  - _Requirements: 10.3, 10.4, 10.5_

- [ ] 11. Implement message scheduling
  - Create IMessageScheduler interface with ScheduleSend, SchedulePublish, CancelScheduledSend methods
  - Implement scheduled message storage in persistent store
  - Implement background service for polling due messages
  - Implement scheduled message delivery at specified time
  - Support recurring schedules using cron expressions
  - Persist scheduled messages to survive restarts
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5_

- [ ] 12. Implement observability infrastructure
  - Implement metrics collection for throughput, latency, error rate, retry count
  - Implement queue depth metrics where supported by transport
  - Create OpenTelemetry instrumentation package
  - Implement AddFlickerFlowInstrumentation extension method
  - Support integration with OpenTelemetry exporters
  - _Requirements: 19.2, 19.3, 19.5_

- [ ] 13. Implement health checks
  - Create FlickerFlow health check implementation
  - Verify transport connection is active
  - Verify receive endpoints are running
  - Check circuit breaker states
  - Implement AddFlickerFlow extension for IHealthChecksBuilder
  - _Requirements: 4.2, 4.3_

- [ ] 14. Implement header management
  - Implement Headers collection with typed value support
  - Implement automatic serialization for complex header values
  - Ensure headers are preserved across message routing
  - Set standard headers (MessageId, CorrelationId, Timestamp) automatically
  - _Requirements: 16.1, 16.2, 16.3, 16.5_

- [ ]* 15. Create sample applications and documentation
  - Create sample console application demonstrating pub/sub pattern
  - Create sample application demonstrating request/response pattern
  - Create sample application demonstrating saga pattern
  - Create README with getting started guide
  - Create API documentation
  - _Requirements: All requirements_

- [ ]* 16. Write integration tests
  - Write integration tests for RabbitMQ transport using Testcontainers
  - Write integration tests for in-memory transport
  - _Requirements: All requirements_


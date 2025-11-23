// This file demonstrates the usage of the DI configuration API
// It is not part of the actual implementation and can be removed

#if false

using FlickerFlow.Abstractions;
using FlickerFlow.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlickerFlow.Core.Configuration.Examples;

// Example message
public class OrderCreated
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

// Example consumer
public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    public Task Consume(ConsumeContext<OrderCreated> context)
    {
        Console.WriteLine($"Order {context.Message.OrderId} created with amount {context.Message.Amount}");
        return Task.CompletedTask;
    }
}

// Example usage
public class ExampleUsage
{
    public void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Add FlickerFlow with in-memory transport
        services.AddFlickerFlow(bus =>
        {
            // Register consumers
            bus.AddConsumer<OrderCreatedConsumer>();

            // Configure transport
            bus.UsingInMemory(cfg =>
            {
                // Configure receive endpoint
                cfg.ReceiveEndpoint("order-queue", ep =>
                {
                    // Bind consumer to endpoint
                    ep.Consumer<OrderCreatedConsumer>();
                    
                    // Configure endpoint settings
                    ep.ConfigurePrefetchCount(20);
                    ep.ConfigureConcurrentMessageLimit(5);
                });
            });

            // Configure shutdown timeout
            bus.ConfigureShutdownTimeout(TimeSpan.FromSeconds(60));
        });

        var serviceProvider = services.BuildServiceProvider();
        var bus = serviceProvider.GetRequiredService<IBus>();
    }

    public void ConfigureServicesWithRabbitMq()
    {
        var services = new ServiceCollection();

        // Add FlickerFlow with RabbitMQ transport
        services.AddFlickerFlow(bus =>
        {
            // Register consumers
            bus.AddConsumer<OrderCreatedConsumer>();

            // Configure RabbitMQ transport
            bus.UsingRabbitMq(cfg =>
            {
                // Configure host
                cfg.Host("localhost", h =>
                {
                    h.Port(5672);
                    h.VirtualHost("/");
                    h.Credentials("guest", "guest");
                });

                // Configure receive endpoint
                cfg.ReceiveEndpoint("order-queue", ep =>
                {
                    ep.Consumer<OrderCreatedConsumer>();
                });
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var bus = serviceProvider.GetRequiredService<IBus>();
    }
}

#endif

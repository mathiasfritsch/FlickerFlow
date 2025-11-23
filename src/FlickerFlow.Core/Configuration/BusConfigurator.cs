using System.Reflection;
using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlickerFlow.Core.Configuration;

/// <summary>
/// Implementation of bus configurator
/// </summary>
internal class BusConfigurator : IBusConfigurator
{
    private readonly IServiceCollection _services;
    private readonly List<Type> _consumerTypes = new();
    private Action<IBusFactoryConfigurator>? _transportConfiguration;
    private Func<IServiceProvider, ITransport>? _transportFactory;
    private TimeSpan _shutdownTimeout = TimeSpan.FromSeconds(30);

    public BusConfigurator(IServiceCollection services)
    {
        _services = services;
    }

    public void AddConsumer<T>() where T : class, IConsumer
    {
        var consumerType = typeof(T);
        
        if (_consumerTypes.Contains(consumerType))
        {
            return;
        }

        _consumerTypes.Add(consumerType);
        
        // Register the consumer in DI
        _services.AddScoped(consumerType);
        
        // Find all IConsumer<TMessage> interfaces implemented by this type
        var consumerInterfaces = consumerType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
            .ToList();

        foreach (var consumerInterface in consumerInterfaces)
        {
            _services.AddScoped(consumerInterface, sp => sp.GetRequiredService(consumerType));
        }
    }

    public void AddConsumers(Assembly assembly)
    {
        var consumerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IConsumer).IsAssignableFrom(t))
            .ToList();

        foreach (var consumerType in consumerTypes)
        {
            var addConsumerMethod = typeof(BusConfigurator)
                .GetMethod(nameof(AddConsumer))!
                .MakeGenericMethod(consumerType);
            
            addConsumerMethod.Invoke(this, null);
        }
    }

    public void UsingRabbitMq(Action<IRabbitMqBusFactoryConfigurator> configure)
    {
        _transportConfiguration = cfg => configure((IRabbitMqBusFactoryConfigurator)cfg);
        _transportFactory = sp =>
        {
            var configurator = new RabbitMqBusFactoryConfigurator(sp);
            configure(configurator);
            return configurator.Build();
        };
    }

    public void UsingInMemory(Action<IInMemoryBusFactoryConfigurator> configure)
    {
        _transportConfiguration = cfg => configure((IInMemoryBusFactoryConfigurator)cfg);
        _transportFactory = sp =>
        {
            var configurator = new InMemoryBusFactoryConfigurator(sp);
            configure(configurator);
            return configurator.Build();
        };
    }

    public void ConfigureShutdownTimeout(TimeSpan timeout)
    {
        _shutdownTimeout = timeout;
    }

    internal void Build()
    {
        if (_transportFactory == null)
        {
            throw new InvalidOperationException("No transport configured. Call UsingRabbitMq or UsingInMemory.");
        }

        // Register the transport
        _services.AddSingleton(_transportFactory);

        // Register the bus
        _services.AddSingleton<IBus>(sp =>
        {
            var transport = sp.GetRequiredService<ITransport>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Bus>>();
            return new Bus(transport, logger, _shutdownTimeout);
        });
    }
}

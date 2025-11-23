using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Configuration;
using FlickerFlow.Core.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlickerFlow.Core.Configuration;

/// <summary>
/// Extension methods for configuring FlickerFlow with dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add FlickerFlow messaging to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFlickerFlow(
        this IServiceCollection services,
        Action<IBusConfigurator> configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        // Register core services
        services.TryAddSingleton<IMessageSerializer, SystemTextJsonSerializer>();

        // Create and configure the bus
        var configurator = new BusConfigurator(services);
        configure(configurator);
        configurator.Build();

        return services;
    }
}

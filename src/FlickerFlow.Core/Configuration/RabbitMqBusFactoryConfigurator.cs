using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Configuration;

namespace FlickerFlow.Core.Configuration;

/// <summary>
/// RabbitMQ transport bus factory configurator
/// </summary>
internal class RabbitMqBusFactoryConfigurator : BusFactoryConfiguratorBase, IRabbitMqBusFactoryConfigurator
{
    private string? _host;
    private int _port = 5672;
    private string _virtualHost = "/";
    private string _username = "guest";
    private string _password = "guest";

    public RabbitMqBusFactoryConfigurator(IServiceProvider serviceProvider) 
        : base(serviceProvider)
    {
    }

    public void Host(string host, Action<IRabbitMqHostConfigurator>? configure = null)
    {
        _host = host;
        
        if (configure != null)
        {
            var hostConfigurator = new RabbitMqHostConfigurator(this);
            configure(hostConfigurator);
        }
    }

    public override ITransport Build()
    {
        if (string.IsNullOrEmpty(_host))
        {
            throw new InvalidOperationException("RabbitMQ host must be configured");
        }

        // This will be implemented when we create the RabbitMQ transport
        // For now, throw to indicate it's not yet implemented
        throw new NotImplementedException("RabbitMQ transport will be implemented in task 7");
    }

    internal void SetPort(int port) => _port = port;
    internal void SetVirtualHost(string virtualHost) => _virtualHost = virtualHost;
    internal void SetCredentials(string username, string password)
    {
        _username = username;
        _password = password;
    }
}

/// <summary>
/// RabbitMQ host configurator implementation
/// </summary>
internal class RabbitMqHostConfigurator : IRabbitMqHostConfigurator
{
    private readonly RabbitMqBusFactoryConfigurator _parent;

    public RabbitMqHostConfigurator(RabbitMqBusFactoryConfigurator parent)
    {
        _parent = parent;
    }

    public void Port(int port)
    {
        _parent.SetPort(port);
    }

    public void VirtualHost(string virtualHost)
    {
        _parent.SetVirtualHost(virtualHost);
    }

    public void Credentials(string username, string password)
    {
        _parent.SetCredentials(username, password);
    }
}

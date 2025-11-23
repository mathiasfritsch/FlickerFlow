namespace FlickerFlow.Abstractions;

/// <summary>
/// Base class for transport configuration
/// </summary>
public abstract class TransportConfiguration
{
    /// <summary>
    /// Connection timeout
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Number of connection retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Shutdown timeout for graceful shutdown
    /// </summary>
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Configuration for connection settings
/// </summary>
public class ConnectionConfiguration
{
    /// <summary>
    /// Host address
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Port number
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Username for authentication
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Virtual host (for RabbitMQ)
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Connection timeout
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 3;
}

/// <summary>
/// Configuration for message topology
/// </summary>
public class TopologyConfiguration
{
    /// <summary>
    /// Exchange name (for RabbitMQ)
    /// </summary>
    public string? ExchangeName { get; set; }

    /// <summary>
    /// Queue name
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Exchange type (fanout, topic, direct, headers)
    /// </summary>
    public string ExchangeType { get; set; } = "topic";

    /// <summary>
    /// Whether the queue/exchange should survive broker restarts
    /// </summary>
    public bool Durable { get; set; } = true;

    /// <summary>
    /// Whether to auto-delete when no longer in use
    /// </summary>
    public bool AutoDelete { get; set; } = false;

    /// <summary>
    /// Additional arguments for topology creation
    /// </summary>
    public Dictionary<string, object> Arguments { get; set; } = new();
}

/// <summary>
/// Configuration for receive endpoint performance
/// </summary>
public class ReceiveEndpointConfiguration
{
    /// <summary>
    /// Number of messages to prefetch from the broker
    /// </summary>
    public int PrefetchCount { get; set; } = 16;

    /// <summary>
    /// Maximum number of concurrent messages to process
    /// </summary>
    public int ConcurrentMessageLimit { get; set; } = 10;

    /// <summary>
    /// Topology configuration
    /// </summary>
    public TopologyConfiguration Topology { get; set; } = new();
}

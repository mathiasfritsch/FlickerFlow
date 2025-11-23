namespace FlickerFlow.Abstractions;

/// <summary>
/// Handle for managing endpoint connections
/// </summary>
public interface ConnectHandle : IDisposable
{
    /// <summary>
    /// Disconnect the endpoint
    /// </summary>
    Task Disconnect();
}

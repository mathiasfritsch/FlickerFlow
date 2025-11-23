namespace FlickerFlow.Abstractions;

/// <summary>
/// Factory for obtaining send endpoints by URI
/// </summary>
public interface ISendEndpointProvider
{
    /// <summary>
    /// Get a send endpoint for the specified address
    /// </summary>
    Task<ISendEndpoint> GetSendEndpoint(Uri address);
}

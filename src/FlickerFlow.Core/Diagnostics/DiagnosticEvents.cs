namespace FlickerFlow.Core.Diagnostics;

/// <summary>
/// Diagnostic event names for distributed tracing
/// </summary>
public static class DiagnosticEvents
{
    /// <summary>
    /// Base name for all FlickerFlow diagnostic events
    /// </summary>
    public const string BaseName = "FlickerFlow";

    /// <summary>
    /// Event emitted when a message publish starts
    /// </summary>
    public const string PublishStart = "FlickerFlow.Publish.Start";

    /// <summary>
    /// Event emitted when a message publish completes
    /// </summary>
    public const string PublishStop = "FlickerFlow.Publish.Stop";

    /// <summary>
    /// Event emitted when a message send starts
    /// </summary>
    public const string SendStart = "FlickerFlow.Send.Start";

    /// <summary>
    /// Event emitted when a message send completes
    /// </summary>
    public const string SendStop = "FlickerFlow.Send.Stop";

    /// <summary>
    /// Event emitted when message consumption starts
    /// </summary>
    public const string ConsumeStart = "FlickerFlow.Consume.Start";

    /// <summary>
    /// Event emitted when message consumption completes
    /// </summary>
    public const string ConsumeStop = "FlickerFlow.Consume.Stop";

    /// <summary>
    /// Event emitted when message consumption fails
    /// </summary>
    public const string ConsumeError = "FlickerFlow.Consume.Error";
}

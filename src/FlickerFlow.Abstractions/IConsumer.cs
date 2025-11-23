namespace FlickerFlow.Abstractions;

/// <summary>
/// Interface for message consumers
/// </summary>
/// <typeparam name="TMessage">The type of message to consume</typeparam>
public interface IConsumer<in TMessage> where TMessage : class
{
    /// <summary>
    /// Consume a message
    /// </summary>
    Task Consume(ConsumeContext<TMessage> context);
}

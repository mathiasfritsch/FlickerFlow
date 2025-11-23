namespace FlickerFlow.Abstractions.Middleware;

/// <summary>
/// Base interface for middleware components in the message processing pipeline
/// </summary>
/// <typeparam name="TContext">The context type that flows through the pipeline</typeparam>
public interface IMiddleware<in TContext>
{
    /// <summary>
    /// Invokes the middleware with the given context
    /// </summary>
    /// <param name="context">The context for the current message</param>
    /// <param name="next">The next middleware in the pipeline</param>
    Task Invoke(TContext context, Func<Task> next);
}

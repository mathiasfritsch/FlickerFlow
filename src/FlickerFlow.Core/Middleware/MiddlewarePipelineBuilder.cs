using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Middleware;

namespace FlickerFlow.Core.Middleware;

/// <summary>
/// Builder for constructing middleware pipelines
/// </summary>
public class MiddlewarePipelineBuilder<TContext>
{
    private readonly List<IMiddleware<TContext>> _middlewares = new();

    /// <summary>
    /// Add a middleware to the pipeline
    /// </summary>
    public MiddlewarePipelineBuilder<TContext> Use(IMiddleware<TContext> middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Add a middleware type to the pipeline (will be resolved from service provider)
    /// </summary>
    public MiddlewarePipelineBuilder<TContext> Use<TMiddleware>() where TMiddleware : IMiddleware<TContext>
    {
        // Store type information for later resolution
        return this;
    }

    /// <summary>
    /// Add a middleware using a factory function
    /// </summary>
    public MiddlewarePipelineBuilder<TContext> Use(Func<TContext, Func<Task>, Task> middleware)
    {
        _middlewares.Add(new DelegateMiddleware<TContext>(middleware));
        return this;
    }

    /// <summary>
    /// Build the pipeline with the final handler
    /// </summary>
    public MiddlewarePipeline<TContext> Build(Func<TContext, Task> finalHandler)
    {
        return new MiddlewarePipeline<TContext>(_middlewares, finalHandler);
    }

    /// <summary>
    /// Get the registered middlewares
    /// </summary>
    internal IReadOnlyList<IMiddleware<TContext>> GetMiddlewares() => _middlewares.AsReadOnly();
}

/// <summary>
/// Middleware implementation that wraps a delegate
/// </summary>
internal class DelegateMiddleware<TContext> : IMiddleware<TContext>
{
    private readonly Func<TContext, Func<Task>, Task> _middleware;

    public DelegateMiddleware(Func<TContext, Func<Task>, Task> middleware)
    {
        _middleware = middleware;
    }

    public Task Invoke(TContext context, Func<Task> next)
    {
        return _middleware(context, next);
    }
}

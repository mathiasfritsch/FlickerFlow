using FlickerFlow.Abstractions;
using FlickerFlow.Abstractions.Middleware;

namespace FlickerFlow.Core.Middleware;

/// <summary>
/// Executes a chain of middleware components
/// </summary>
public class MiddlewarePipeline<TContext>
{
    private readonly Func<TContext, Task> _pipeline;

    public MiddlewarePipeline(IEnumerable<IMiddleware<TContext>> middlewares, Func<TContext, Task> finalHandler)
    {
        _pipeline = BuildPipeline(middlewares.Reverse(), finalHandler);
    }

    /// <summary>
    /// Execute the pipeline with the given context
    /// </summary>
    public Task Execute(TContext context)
    {
        return _pipeline(context);
    }

    private static Func<TContext, Task> BuildPipeline(
        IEnumerable<IMiddleware<TContext>> middlewares,
        Func<TContext, Task> finalHandler)
    {
        var current = finalHandler;

        foreach (var middleware in middlewares)
        {
            var next = current;
            current = context => middleware.Invoke(context, () => next(context));
        }

        return current;
    }
}

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Domain;
using Miniclip.Core.ReadModels.Projections.Attributes;

namespace Miniclip.Core.ReadModels.Projections;

public static class ProjectionServiceCollectionExtensions
{
    // Compiled once per handler type for the lifetime of the process — zero MethodInfo.Invoke at dispatch time.
    private static readonly ConcurrentDictionary<Type, Func<object, IDomainEvent, CancellationToken, ValueTask>>
        DelegateCache = new();

    public static IServiceCollection AddProjectionHandlers(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var handlerInterface = typeof(IProjectionHandler<>);

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
            {
                foreach (var iface in type.GetInterfaces())
                {
                    if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != handlerInterface)
                        continue;

                    var eventType = iface.GetGenericArguments()[0];
                    var priority  = type.GetCustomAttribute<HandlerHighPriorityAttribute>()?.Priority ?? 0;
                    var compiled  = DelegateCache.GetOrAdd(type, t => BuildDelegate(t, eventType));

                    // Register concrete handler so the DI container can construct it (inject repositories, etc.)
                    services.AddScoped(type);

                    // Register bridge that the dispatcher receives via IEnumerable<IProjectionHandler>
                    services.AddScoped<IProjectionHandler>(sp =>
                    {
                        var handler = sp.GetRequiredService(type);
                        return new CompiledProjectionHandler(eventType, priority, compiled, handler);
                    });
                }
            }
        }

        services.AddScoped<IProjectionDispatcher, ProjectionDispatcher>();
        return services;
    }

    private static Func<object, IDomainEvent, CancellationToken, ValueTask> BuildDelegate(
        Type handlerType, Type eventType)
    {
        // (object rawHandler, IDomainEvent rawEvent, CancellationToken ct)
        //   => ((THandler)rawHandler).HandleAsync((TEvent)rawEvent, ct)
        var handlerParam = Expression.Parameter(typeof(object),          "rawHandler");
        var eventParam   = Expression.Parameter(typeof(IDomainEvent),    "rawEvent");
        var ctParam      = Expression.Parameter(typeof(CancellationToken), "ct");

        var typedHandler = Expression.Convert(handlerParam, handlerType);
        var typedEvent   = Expression.Convert(eventParam,   eventType);

        var handleAsync = handlerType.GetMethod(nameof(IProjectionHandler<IDomainEvent>.HandleAsync))!;
        var callExpr    = Expression.Call(typedHandler, handleAsync, typedEvent, ctParam);

        return Expression
            .Lambda<Func<object, IDomainEvent, CancellationToken, ValueTask>>(
                callExpr, handlerParam, eventParam, ctParam)
            .Compile();
    }
}

// Internal bridge: wraps a concrete handler instance + pre-compiled delegate.
// Registered as IProjectionHandler in DI — received by ProjectionDispatcher via constructor injection.
internal sealed class CompiledProjectionHandler(
    Type eventType,
    int priority,
    Func<object, IDomainEvent, CancellationToken, ValueTask> compiledDelegate,
    object handlerInstance) : IProjectionHandler
{
    public Type  EventType => eventType;
    public int   Priority  => priority;

    public ValueTask HandleAsync(IDomainEvent @event, CancellationToken ct) =>
        compiledDelegate(handlerInstance, @event, ct);
}

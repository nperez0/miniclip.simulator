using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Messaging;
using Miniclip.Core.ReadModels.Projections.Attributes;

namespace Miniclip.Core.ReadModels.Projections.Configuration;

public static class ProjectionsConfiguration
{
    public static IServiceCollection AddProjectionHandlers(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var handlerInterface = typeof(IProjectionHandler<>);

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
            {
                foreach (var @interface in type.GetInterfaces())
                {
                    if (!@interface.IsGenericType || @interface.GetGenericTypeDefinition() != handlerInterface)
                        continue;

                    var eventType = @interface.GetGenericArguments()[0];
                    var priority  = type.GetCustomAttribute<HandlerHighPriorityAttribute>()?.Priority ?? 0;
                    var compiled  = HandlerInvokerFactory.GetDelegate(type, eventType);

                    services.AddScoped(type);

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

    private static class HandlerInvokerFactory
    {
        private static readonly MethodInfo GetDelegateMethod =
            typeof(HandlerInvokerFactory).GetMethod(nameof(GetDelegateCore), BindingFlags.Static | BindingFlags.NonPublic)!;

        internal static Func<object, IIntegrationEvent, CancellationToken, ValueTask> GetDelegate(Type handlerType, Type eventType) =>
            (Func<object, IIntegrationEvent, CancellationToken, ValueTask>)
                GetDelegateMethod.MakeGenericMethod(handlerType, eventType).Invoke(null, null)!;

        private static Func<object, IIntegrationEvent, CancellationToken, ValueTask> GetDelegateCore<THandler, TEvent>()
            where THandler : class, IProjectionHandler<TEvent>
            where TEvent : IIntegrationEvent =>
            HandlerInvoker<THandler, TEvent>.Invoke;
    }

    // CLR generic type system acts as the cache — Invoke is compiled exactly once per (THandler, TEvent) pair.
    private static class HandlerInvoker<THandler, TEvent>
        where THandler : class, IProjectionHandler<TEvent>
        where TEvent : IIntegrationEvent
    {
        public static readonly Func<object, IIntegrationEvent, CancellationToken, ValueTask> Invoke =
            static (rawHandler, rawEvent, ct) =>
                ((THandler)rawHandler).HandleAsync((TEvent)rawEvent, ct);
    }
}

// Internal bridge: wraps a concrete handler instance + pre-compiled delegate.
// Registered as IProjectionHandler in DI — received by ProjectionDispatcher via constructor injection.
internal sealed class CompiledProjectionHandler(
    Type eventType,
    int priority,
    Func<object, IIntegrationEvent, CancellationToken, ValueTask> compiledDelegate,
    object handlerInstance) : IProjectionHandler
{
    public Type  EventType => eventType;
    public int   Priority  => priority;

    public ValueTask HandleAsync(IIntegrationEvent @event, CancellationToken cancellationToken) =>
        compiledDelegate(handlerInstance, @event, cancellationToken);
}

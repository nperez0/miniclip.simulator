using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Messaging.Pipeline.Middleware;

namespace Miniclip.Core.Messaging.Pipeline.Configuration;

public static class PipelineConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMessagingPipeline(Action<PipelineOptions>? configure = null)
        {
            var options = new PipelineOptions();
            configure?.Invoke(options);

            // Register middleware (order matters: first registered = outermost)
            services.AddSingleton<IMessageMiddleware, TracingMiddleware>();
            services.AddSingleton<IMessageMiddleware, LoggingMiddleware>();
            services.AddSingleton<IMessageMiddleware, RetryMiddleware>();

            services.AddSingleton(options.RetryPolicy);
            services.AddSingleton<IMessagePipeline, MessagePipeline>();

            services.AddSingleton<IMessageHandlerRegistry>(sp =>
                new MessageHandlerRegistry(sp.GetServices<CompiledMessageHandler>()));

            return services;
        }

        public IServiceCollection AddMessageHandlers(params Assembly[] assemblies)
        {
            if (assemblies.Length == 0)
                assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.GetName().Name?.StartsWith("Miniclip") == true)
                    .ToArray();

            var handlerInterface = typeof(IMessageHandler<>);
            var allTypes = assemblies.SelectMany(a => a.GetTypes()).ToList();
            var concreteTypes = allTypes.Where(t => t is { IsAbstract: false, IsInterface: false }).ToList();

            foreach (var type in concreteTypes)
            {
                if (type.IsGenericTypeDefinition)
                {
                    // Open generic handler (e.g. ProjectionMessageHandler<TEvent> : IMessageHandler<TEvent>)
                    // Close it for every concrete message type satisfying the constraint.
                    var openHandlerInterface = type.GetInterfaces()
                        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface);

                    if (openHandlerInterface is null)
                        continue;

                    var constraints = type.GetGenericArguments()[0].GetGenericParameterConstraints();
                    var concreteMessageTypes = allTypes
                        .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }
                                    && constraints.All(c => c.IsAssignableFrom(t)));

                    foreach (var concreteMessageType in concreteMessageTypes)
                    {
                        var closedHandlerType = type.MakeGenericType(concreteMessageType);
                        var invoke = MessageHandlerRegistry.BuildDelegate(closedHandlerType, concreteMessageType);

                        services.AddSingleton(new CompiledMessageHandler(concreteMessageType, closedHandlerType, invoke));
                        services.AddScoped(closedHandlerType);
                    }
                }
                else
                {
                    foreach (var @interface in type.GetInterfaces())
                    {
                        if (!@interface.IsGenericType || @interface.GetGenericTypeDefinition() != handlerInterface)
                            continue;

                        var messageType = @interface.GetGenericArguments()[0];
                        var invoke = MessageHandlerRegistry.BuildDelegate(type, messageType);

                        services.AddSingleton(new CompiledMessageHandler(messageType, type, invoke));
                        services.AddScoped(type);
                    }
                }
            }

            return services;
        }
    }
}

public class PipelineOptions
{
    public IRetryPolicy RetryPolicy { get; set; } = new ExponentialBackoffRetryPolicy(maxAttempts: 3);
}
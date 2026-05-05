using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Messaging.Pipeline.Inbound;
using Miniclip.Core.Messaging.Pipeline.Inbound.Middleware;
using Miniclip.Core.Messaging.Pipeline.Outbound;
using Miniclip.Core.Messaging.Pipeline.Outbound.Middleware;

namespace Miniclip.Core.Messaging.Pipeline.Configuration;

public static class PipelineConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInboundPipeline(Action<PipelineOptions>? configure = null)
        {
            var options = new PipelineOptions();
            configure?.Invoke(options);

            // Inbound middleware (outermost first). PropagationMiddleware is Scoped; others are Singleton.
            services.AddScoped<IInboundMiddleware, PropagationMiddleware>();
            services.AddSingleton<IInboundMiddleware, TracingMiddleware>();
            services.AddSingleton<IInboundMiddleware, LoggingMiddleware>();
            services.AddSingleton<IInboundMiddleware, RetryMiddleware>();

            services.AddSingleton(options.RetryPolicy);
            services.AddSingleton<IInboundPipeline, MessagePipeline>();

            services.AddSingleton<IMessageHandlerRegistry>(sp =>
            {
                var handlers = sp.GetServices<CompiledMessageHandler>().ToArray();
                
                return new MessageHandlerRegistry(handlers);
            });

            return services;
        }

        public IServiceCollection AddOutboundPipeline()
        {
            // Propagation context (scoped - one per message scope)
            services.AddScoped<PropagationContext>();
            services.AddScoped<IPropagationContext>(sp => sp.GetRequiredService<PropagationContext>());
            services.AddScoped<IMutablePropagationContext>(sp => sp.GetRequiredService<PropagationContext>());

            // Outbound middleware (outermost first). PropagationEnrichmentMiddleware is Scoped; OutboundTracingMiddleware is Singleton.
            services.AddScoped<IOutboundMiddleware, PropagationEnrichmentMiddleware>();
            services.AddSingleton<IOutboundMiddleware, OutboundTracingMiddleware>();

            // IEventBus is Scoped so it resolves IOutboundMiddleware (and Scoped PropagationEnrichmentMiddleware) from the caller's scope.
            services.AddScoped<IEventBus, OutboundPipeline>();

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

    // Builds a registry scoped to only the message types declared in the subscription.
    // Called by transport-specific wiring (e.g. AddKafkaConsumer) to give each
    // consumer host its own filtered view of the global handler pool.
    public static IMessageHandlerRegistry BuildFilteredRegistry(
        ConsumerSubscription subscription,
        IEnumerable<CompiledMessageHandler> allHandlers)
    {
        var declared = subscription.MessageTypes.ToHashSet();

        var filtered = allHandlers
            .Where(h => declared.Contains(h.MessageType))
            .ToArray();

        return new MessageHandlerRegistry(filtered);
    }
}

public class PipelineOptions
{
    public IRetryPolicy RetryPolicy { get; set; } = new ExponentialBackoffRetryPolicy(maxAttempts: 3);
}
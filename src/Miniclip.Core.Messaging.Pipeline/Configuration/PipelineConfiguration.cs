using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Messaging.Pipeline.Inbound;
using Miniclip.Core.Messaging.Pipeline.Inbound.Middleware;
using Miniclip.Core.Messaging.Pipeline.Outbound;
using Miniclip.Core.Messaging.Pipeline.Outbound.Middleware;
using Miniclip.Core.Reflection;

namespace Miniclip.Core.Messaging.Pipeline.Configuration;

public static class PipelineConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOutboundPipeline()
        {
            // Propagation context (scoped - one per message scope)
            services.AddScoped<PropagationContext>();
            services.AddScoped<IPropagationContext>(sp => sp.GetRequiredService<PropagationContext>());
            services.AddScoped<IMutablePropagationContext>(sp => sp.GetRequiredService<PropagationContext>());

            // Outbound middleware (outermost first). PropagationEnrichmentMiddleware is Scoped; OutboundTracingMiddleware is Singleton.
            services.AddScoped<IOutboundMiddleware, PropagationEnrichmentMiddleware>();
            services.AddSingleton<IOutboundMiddleware, OutboundTracingMiddleware>();

            return services;
        }

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

        public IServiceCollection AddMessageHandlers(params Assembly[] assemblies)
        {
            var concreteTypes = assemblies.Length > 0
                ? assemblies
                    .SelectMany(a => a.GetTypes())
                    .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
                    .ToArray()
                : AssemblyScanner.GetConcreteTypes().ToArray();

            var handlerInterface = typeof(IMessageHandler<>);

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
                    var concreteMessageTypes = concreteTypes
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

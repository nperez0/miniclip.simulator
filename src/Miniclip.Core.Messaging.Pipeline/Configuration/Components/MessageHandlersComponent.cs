using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Miniclip.Core.Messaging.Configuration;
using Miniclip.Core.Messaging.Pipeline.Inbound;
using Miniclip.Core.Reflection;

namespace Miniclip.Core.Messaging.Pipeline.Configuration.Components;

public sealed class MessageHandlersComponent(params Assembly[] assemblies) : IMessagingComponent
{
    public void Register(IServiceCollection services)
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

        services.TryAddSingleton<IMessageHandlerRegistry>(sp =>
        {
            var handlers = sp.GetServices<CompiledMessageHandler>().ToArray();
            return new MessageHandlerRegistry(handlers);
        });
    }
}


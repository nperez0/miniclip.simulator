using System.Collections.Frozen;
using System.Reflection;
using Miniclip.Core.Messaging;

namespace Miniclip.Core.Messaging.Pipeline.Inbound;

public sealed class MessageHandlerRegistry : IMessageHandlerRegistry
{
    private readonly FrozenDictionary<string, CompiledMessageHandler> handlers;
        
    internal MessageHandlerRegistry(IEnumerable<CompiledMessageHandler> compiled)
    {
        var compiledMessageHandlers = compiled as CompiledMessageHandler[] ?? compiled.ToArray();

        MessageHandlerRegistryGuard.Validate(compiledMessageHandlers);

        handlers = compiledMessageHandlers.ToFrozenDictionary(h => h.MessageType.GetMessageTypeName());
    }

    public CompiledMessageHandler? TryGet(string messageTypeName) =>
        handlers.GetValueOrDefault(messageTypeName);

    internal static Func<object, object, IMessageContext, CancellationToken, Task<MessageHandlerResult>> BuildDelegate(
        Type handlerType,
        Type messageType)
    {
        var invokerType = typeof(HandlerInvoker<,>).MakeGenericType(handlerType, messageType);
        var field = invokerType.GetField(nameof(HandlerInvoker<,>.Invoke),
            BindingFlags.Static | BindingFlags.Public)!;

        return (Func<object, object, IMessageContext, CancellationToken, Task<MessageHandlerResult>>)field.GetValue(null)!;
    }

    // CLR generic type system acts as the cache - Invoke is compiled exactly once per (THandler, TMessage) pair.
    private static class HandlerInvoker<THandler, TMessage>
        where THandler : class, IMessageHandler<TMessage>
    {
        public static readonly Func<object, object, IMessageContext, CancellationToken, Task<MessageHandlerResult>> Invoke =
            static (rawHandler, rawMessage, ctx, ct) =>
                ((THandler)rawHandler).HandleAsync((TMessage)rawMessage, ctx, ct);
    }
}
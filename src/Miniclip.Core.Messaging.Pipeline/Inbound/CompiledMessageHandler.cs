
namespace Miniclip.Core.Messaging.Pipeline.Inbound;

public sealed class CompiledMessageHandler(
    Type messageType,
    Type handlerType,
    Func<object, object, IMessageContext, CancellationToken, Task<MessageHandlerResult>> invoke)
{
    public Type MessageType { get; } = messageType;

    public Type HandlerType { get; } = handlerType;

    public Task<MessageHandlerResult> InvokeAsync(
        object handlerInstance,
        object message,
        IMessageContext context,
        CancellationToken cancellationToken) =>
        invoke(handlerInstance, message, context, cancellationToken);
}

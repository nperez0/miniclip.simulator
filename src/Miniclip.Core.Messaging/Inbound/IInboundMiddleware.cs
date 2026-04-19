namespace Miniclip.Core.Messaging.Inbound;

public interface IInboundMiddleware
{
    Task<MessageHandlerResult> InvokeAsync(
        IMessageEnvelope envelope,
        IMessageContext context,
        Func<Task<MessageHandlerResult>> next,
        CancellationToken cancellationToken);
}

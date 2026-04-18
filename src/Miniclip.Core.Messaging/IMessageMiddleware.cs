namespace Miniclip.Core.Messaging;

public interface IMessageMiddleware
{
    Task<MessageHandlerResult> InvokeAsync(
        IMessageEnvelope envelope,
        IMessageContext context,
        Func<Task<MessageHandlerResult>> next,
        CancellationToken cancellationToken);
}

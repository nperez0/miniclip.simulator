namespace Miniclip.Core.Messaging.Inbound;

public interface IMessageHandler<in TMessage>
{
    Task<MessageHandlerResult> HandleAsync(
        TMessage message,
        IMessageContext context,
        CancellationToken cancellationToken);
}

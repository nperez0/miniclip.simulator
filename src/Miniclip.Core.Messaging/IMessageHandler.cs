namespace Miniclip.Core.Messaging;

public interface IMessageHandler<in TMessage>
{
    Task<MessageHandlerResult> HandleAsync(
        TMessage message,
        IMessageContext context,
        CancellationToken cancellationToken);
}

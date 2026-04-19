namespace Miniclip.Core.Messaging.Inbound;

public interface IDeadLetterHandler
{
    Task HandleAsync(
        IMessageEnvelope envelope,
        string reason,
        Exception? exception,
        CancellationToken cancellationToken);
}

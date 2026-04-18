namespace Miniclip.Core.Messaging;

public interface IDeadLetterHandler
{
    Task HandleAsync(
        IMessageEnvelope envelope,
        string reason,
        Exception? exception,
        CancellationToken cancellationToken);
}

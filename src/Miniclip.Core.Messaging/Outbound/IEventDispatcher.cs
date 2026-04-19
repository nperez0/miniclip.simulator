namespace Miniclip.Core.Messaging.Outbound;

public interface IEventDispatcher
{
    Task DispatchAsync(OutboundEnvelope envelope, CancellationToken cancellationToken);
}


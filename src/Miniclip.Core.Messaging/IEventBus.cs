using Miniclip.Core.EventSourcing;

namespace Miniclip.Core.Messaging;

public interface IEventBus
{
    Task PublishAsync(CommittedEvent committed, CancellationToken cancellationToken = default);
}

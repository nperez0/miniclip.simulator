using Miniclip.Core.EventSourcing;

namespace Miniclip.Core.Application;

public interface IEventBus
{
    Task PublishAsync(CommittedEvent committed, CancellationToken cancellationToken = default);
}

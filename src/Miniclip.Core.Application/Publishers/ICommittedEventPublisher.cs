using Miniclip.Core.EventSourcing;

namespace Miniclip.Core.Application.Publishers;

public interface ICommittedEventPublisher
{
    Task PublishAsync(CommittedEvent committed, CancellationToken cancellationToken = default);
}

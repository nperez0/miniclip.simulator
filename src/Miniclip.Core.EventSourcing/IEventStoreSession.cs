using Miniclip.Core.Domain;

namespace Miniclip.Core.EventSourcing;

public interface IEventStoreSession
{
    void Track(Func<CancellationToken, Task<IDomainEvent[]>> commitAction);

    Task CommitAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<IDomainEvent> GetCommittedEvents();
}

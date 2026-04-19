using Miniclip.Core.Domain;

namespace Miniclip.Core.EventSourcing;

public readonly record struct CommittedEvent(
    IDomainEvent Event,
    Guid AggregateId,
    string AggregateType,
    Guid EventId,
    DateTimeOffset OccurredOn,
    long AggregateVersion);

public interface IEventStoreSession
{
    void Track(Func<CancellationToken, Task<CommittedEvent[]>> commitAction);

    Task CommitAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<CommittedEvent> GetCommittedEvents();
}

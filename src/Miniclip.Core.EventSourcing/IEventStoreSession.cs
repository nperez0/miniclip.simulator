namespace Miniclip.Core.EventSourcing;

public interface IEventStoreSession
{
    void Track(Func<CancellationToken, Task<CommittedEvent[]>> commitAction);

    Task CommitAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<CommittedEvent> GetCommittedEvents();
}

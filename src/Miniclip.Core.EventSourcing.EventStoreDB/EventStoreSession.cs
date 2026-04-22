
namespace Miniclip.Core.EventSourcing.EventStoreDB;

public sealed class EventStoreSession : IEventStoreSession
{
    private readonly List<Func<CancellationToken, Task<CommittedEvent[]>>> pendingActions = [];
    private readonly List<CommittedEvent> committedEvents = [];

    public void Track(Func<CancellationToken, Task<CommittedEvent[]>> commitAction)
        => pendingActions.Add(commitAction);

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        foreach (var action in pendingActions)
            committedEvents.AddRange(await action(cancellationToken));

        pendingActions.Clear();
    }

    public IReadOnlyList<CommittedEvent> GetCommittedEvents()
        => committedEvents.AsReadOnly();
}

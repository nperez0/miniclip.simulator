using Miniclip.Core.Domain;

namespace Miniclip.Core.EventSourcing.EventStoreDB;

public sealed class EventStoreSession : IEventStoreSession
{
    private readonly List<Func<CancellationToken, Task<IDomainEvent[]>>> pendingActions = [];
    private readonly List<IDomainEvent> committedEvents = [];

    public void Track(Func<CancellationToken, Task<IDomainEvent[]>> commitAction)
        => pendingActions.Add(commitAction);

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        foreach (var action in pendingActions)
        {
            var events = await action(cancellationToken);
            committedEvents.AddRange(events);
        }

        pendingActions.Clear();
    }

    public IReadOnlyList<IDomainEvent> GetCommittedEvents()
        => committedEvents.AsReadOnly();
}

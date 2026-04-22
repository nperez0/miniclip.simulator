namespace Miniclip.Core.EventSourcing;

public class AggregateRepository<T>(IEventStore<T> eventStore) : IAggregateRepository<T>
    where T : AggregateRoot
{
    public void Add(T aggregate)
        => eventStore.Track(aggregate);

    public async Task<T?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        => await eventStore.LoadAsync(id, cancellationToken);

    public Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken)
        => eventStore.GetAllAsync(cancellationToken);
}

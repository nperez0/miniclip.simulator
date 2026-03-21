using Miniclip.Core.Domain;

namespace Miniclip.Core.EventSourcing;

public class EventSourcedRepository<T>(IEventStore<T> eventStore) : IRepository<T>
    where T : AggregateRoot
{
    public void Add(T aggregate)
        => eventStore.Track(aggregate);

    public async Task<T?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        => await eventStore.LoadAsync(id, cancellationToken);

    public Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException($"{nameof(EventSourcedRepository<T>)} does not support {nameof(GetAllAsync)}.");

    public void Delete(T aggregate)
        => throw new NotSupportedException($"{nameof(EventSourcedRepository<T>)} does not support {nameof(Delete)}.");
}

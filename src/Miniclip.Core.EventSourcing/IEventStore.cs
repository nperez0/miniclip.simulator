using Miniclip.Core.Domain;

namespace Miniclip.Core.EventSourcing;

public interface IEventStore<T> where T : AggregateRoot
{
    void Track(T aggregate);

    Task<T?> LoadAsync(Guid aggregateId, CancellationToken cancellationToken = default);
}

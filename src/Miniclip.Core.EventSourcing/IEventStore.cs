using Miniclip.Core.Domain;

namespace Miniclip.Core.EventSourcing;

public interface IEventStore<T> where T : AggregateRoot
{
    Task AppendAsync(T aggregate, CancellationToken cancellationToken = default);

    Task<T?> LoadAsync(Guid aggregateId, CancellationToken cancellationToken = default);
}

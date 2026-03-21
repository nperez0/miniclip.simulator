namespace Miniclip.Core.Domain;

public interface IAggregateRepository<T> where T : AggregateRoot
{
    Task<T?> FindAsync(Guid id, CancellationToken cancellationToken);

    void Add(T aggregate);

    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken);
}

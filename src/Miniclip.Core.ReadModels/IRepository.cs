namespace Miniclip.Core.ReadModels;

public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken);

    Task<T?> FindAsync(Guid id, CancellationToken cancellationToken);

    void Add(T entity);

    void Delete(T entity);
}

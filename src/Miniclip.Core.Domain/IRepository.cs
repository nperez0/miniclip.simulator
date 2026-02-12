namespace Miniclip.Core.Domain;

public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAll();

    Task<T> FindAsync(Guid id);

    void Add(T entity);

    void Delete(T entity);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

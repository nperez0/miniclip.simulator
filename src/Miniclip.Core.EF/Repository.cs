using Microsoft.EntityFrameworkCore;
using Miniclip.Core.Domain;

namespace Miniclip.Core.EF;

public class Repository<T>(DbContext context) : IRepository<T>
    where T : class
{
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken)
        => await context
            .Set<T>()
            .ToListAsync(cancellationToken);

    public virtual Task<T?> FindAsync(Guid id, CancellationToken cancellationToken)
        => context
            .Set<T>()
            .FindAsync([id], cancellationToken: cancellationToken)
            .AsTask();

    public virtual void Add(T entity)
    {
        context
            .Set<T>()
            .Add(entity);
    }

    public virtual void Delete(T entity)
    {
        context.Entry(entity).State = EntityState.Deleted;
    }
}

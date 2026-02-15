using Microsoft.EntityFrameworkCore;
using Miniclip.Core.ReadModels;
using System.Linq.Expressions;

namespace Miniclip.Core.EF;

public class ReadModelRepository<T>(DbContext context) 
    : IReadOnlyRepository<T> where T : class
{
    protected DbContext Context => context;

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await context
            .Set<T>()
            .FindAsync([id], cancellationToken);

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken)
        => await context
            .Set<T>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    protected async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) 
        => await context.Set<T>()
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken);
}

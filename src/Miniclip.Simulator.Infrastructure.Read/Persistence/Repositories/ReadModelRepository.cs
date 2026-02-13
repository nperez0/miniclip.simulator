using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Miniclip.Core.ReadModels;
using Miniclip.Simulator.Infrastructure.Read.Persistence;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories;

/// <summary>
/// Base repository implementation for read models.
/// Provides common read operations using EF Core.
/// </summary>
public class ReadModelRepository<T> : IReadOnlyRepository<T> where T : class
{
    protected readonly SimulatorReadDbContext Context;
    protected readonly DbSet<T> DbSet;

    public ReadModelRepository(SimulatorReadDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    protected async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    // Write operations for projections (not exposed through IReadOnlyRepository)
    protected async Task UpsertAsync(T model, CancellationToken cancellationToken = default)
    {
        var entry = Context.Entry(model);
        
        if (entry.State == EntityState.Detached)
        {
            DbSet.Update(model);
        }

        await Context.SaveChangesAsync(cancellationToken);
    }

    protected async Task UpsertManyAsync(IEnumerable<T> models, CancellationToken cancellationToken = default)
    {
        DbSet.UpdateRange(models);
        await Context.SaveChangesAsync(cancellationToken);
    }

    protected async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await DbSet.FindAsync(new object[] { id }, cancellationToken);
        if (entity != null)
        {
            DbSet.Remove(entity);
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    protected async Task DeleteManyAsync(
        Expression<Func<T, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        var entities = await DbSet.Where(predicate).ToListAsync(cancellationToken);
        DbSet.RemoveRange(entities);
        await Context.SaveChangesAsync(cancellationToken);
    }
}

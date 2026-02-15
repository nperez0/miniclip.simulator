using Miniclip.Core.Domain;

namespace Miniclip.Simulator.Infrastructure.Write.Persistence;

public class SimulatorUnitOfWork(SimulatorWriteDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken) 
        => await context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken) 
        => await context.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitAsync(CancellationToken cancellationToken) 
        => await context.Database.CommitTransactionAsync(cancellationToken);

    public async Task RollbackAsync(CancellationToken cancellationToken) 
        => await context.Database.RollbackTransactionAsync(cancellationToken);

    public AggregateRoot[] GetTrackedAggregates()
        => context.ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged)
            .Select(e => e.Entity)
            .ToArray();

    public void Dispose()
    {
        context.Dispose();
    }
}

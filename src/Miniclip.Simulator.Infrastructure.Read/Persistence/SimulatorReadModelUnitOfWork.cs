using Miniclip.Core.ReadModels;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence;

public class SimulatorReadModelUnitOfWork(SimulatorReadDbContext context) : IReadModelUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
        => await context.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitAsync(CancellationToken cancellationToken)
        => await context.Database.CommitTransactionAsync(cancellationToken);

    public async Task RollbackAsync(CancellationToken cancellationToken)
        => await context.Database.RollbackTransactionAsync(cancellationToken);

    public void Dispose()
    {
        context.Dispose();
    }
}

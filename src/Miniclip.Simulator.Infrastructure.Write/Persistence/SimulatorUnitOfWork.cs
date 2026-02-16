using Miniclip.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
    {
        var aggregatesWithChanges = new HashSet<AggregateRoot>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Unchanged)
                continue;

            // If the changed entity is an AggregateRoot itself, add it
            if (entry.Entity is AggregateRoot aggregate)
            {
                aggregatesWithChanges.Add(aggregate);
                continue;
            }

            // Otherwise, find any AggregateRoot that references this changed entity
            FindReferencingAggregates(entry, aggregatesWithChanges);
        }

        return [.. aggregatesWithChanges];
    }

    private void FindReferencingAggregates(EntityEntry changedEntry, HashSet<AggregateRoot> aggregates)
    {
        // Check all tracked entries to find AggregateRoots that reference the changed entity
        foreach (var entry in context.ChangeTracker.Entries<AggregateRoot>())
        {
            // Check all navigation properties of the aggregate
            foreach (var navigation in entry.Navigations)
            {
                if (navigation.Metadata.IsCollection)
                {
                    // Check if the changed entity is in a collection navigation
                    if (navigation.CurrentValue is System.Collections.IEnumerable collection)
                    {
                        foreach (var item in collection)
                        {
                            if (ReferenceEquals(item, changedEntry.Entity))
                            {
                                aggregates.Add(entry.Entity);
                                return;
                            }
                        }
                    }
                }
                else
                {
                    // Check if the changed entity is referenced by a single navigation
                    if (ReferenceEquals(navigation.CurrentValue, changedEntry.Entity))
                    {
                        aggregates.Add(entry.Entity);
                        return;
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        context.Dispose();
    }
}

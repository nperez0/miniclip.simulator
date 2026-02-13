using Microsoft.EntityFrameworkCore;
using Miniclip.Simulator.Infrastructure.Read.Persistence;
using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Repositories;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories;

/// <summary>
/// Repository for GroupOverviewReadModel with specialized query methods.
/// </summary>
public class GroupOverviewRepository : ReadModelRepository<GroupOverviewReadModel>, IGroupOverviewRepository
{
    public GroupOverviewRepository(SimulatorReadDbContext context) : base(context)
    {
    }

    public async Task<GroupOverviewReadModel?> GetOverviewByGroupIdAsync(
        Guid groupId, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GroupId == groupId, cancellationToken);
    }

    public async Task<IEnumerable<GroupOverviewReadModel>> GetAllOverviewsAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .OrderBy(x => x.GroupName)
            .ToListAsync(cancellationToken);
    }

    // Expose write operations for projections
    public new async Task UpsertAsync(GroupOverviewReadModel model, CancellationToken cancellationToken = default)
    {
        await base.UpsertAsync(model, cancellationToken);
    }

    public new async Task UpsertManyAsync(IEnumerable<GroupOverviewReadModel> models, CancellationToken cancellationToken = default)
    {
        await base.UpsertManyAsync(models, cancellationToken);
    }

    public new async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await base.DeleteAsync(id, cancellationToken);
    }

    public new async Task DeleteManyAsync(System.Linq.Expressions.Expression<Func<GroupOverviewReadModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await base.DeleteManyAsync(predicate, cancellationToken);
    }
}

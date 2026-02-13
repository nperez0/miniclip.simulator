using Microsoft.EntityFrameworkCore;
using Miniclip.Simulator.Infrastructure.Read.Persistence;
using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Repositories;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories;

/// <summary>
/// Repository for MatchResultReadModel with specialized query methods.
/// </summary>
public class MatchResultRepository : ReadModelRepository<MatchResultReadModel>, IMatchResultRepository
{
    public MatchResultRepository(SimulatorReadDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<MatchResultReadModel>> GetMatchesByGroupIdAsync(
        Guid groupId, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.GroupId == groupId)
            .OrderBy(x => x.Round)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MatchResultReadModel>> GetMatchesByRoundAsync(
        Guid groupId, 
        int round, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.GroupId == groupId && x.Round == round)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MatchResultReadModel>> GetMatchesByTeamIdAsync(
        Guid teamId, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.HomeTeamId == teamId || x.AwayTeamId == teamId)
            .OrderBy(x => x.GroupId)
            .ThenBy(x => x.Round)
            .ToListAsync(cancellationToken);
    }

    // Expose write operations for projections
    public new async Task UpsertAsync(MatchResultReadModel model, CancellationToken cancellationToken = default)
    {
        await base.UpsertAsync(model, cancellationToken);
    }

    public new async Task UpsertManyAsync(IEnumerable<MatchResultReadModel> models, CancellationToken cancellationToken = default)
    {
        await base.UpsertManyAsync(models, cancellationToken);
    }

    public new async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await base.DeleteAsync(id, cancellationToken);
    }

    public new async Task DeleteManyAsync(System.Linq.Expressions.Expression<Func<MatchResultReadModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await base.DeleteManyAsync(predicate, cancellationToken);
    }
}

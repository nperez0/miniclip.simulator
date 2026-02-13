using Microsoft.EntityFrameworkCore;
using Miniclip.Simulator.Infrastructure.Read.Persistence;
using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Repositories;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories;

/// <summary>
/// Repository for GroupStandingsReadModel with specialized query methods.
/// </summary>
public class GroupStandingsRepository : ReadModelRepository<GroupStandingsReadModel>, IGroupStandingsRepository
{
    public GroupStandingsRepository(SimulatorReadDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<GroupStandingsReadModel>> GetStandingsByGroupIdAsync(
        Guid groupId, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.GroupId == groupId)
            .OrderBy(x => x.Position)
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupStandingsReadModel?> GetTeamStandingAsync(
        Guid groupId, 
        Guid teamId, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.TeamId == teamId, cancellationToken);
    }

    public async Task<IEnumerable<GroupStandingsReadModel>> GetQualifiedTeamsAsync(
        Guid groupId, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.GroupId == groupId && x.QualifiesForKnockout)
            .OrderBy(x => x.Position)
            .ToListAsync(cancellationToken);
    }

    public async Task RebuildStandingsAsync(
        Guid groupId, 
        IEnumerable<GroupStandingsReadModel> standings, 
        CancellationToken cancellationToken = default)
    {
        // Delete old standings for this group
        var oldStandings = await DbSet
            .Where(x => x.GroupId == groupId)
            .ToListAsync(cancellationToken);

        DbSet.RemoveRange(oldStandings);

        // Add new standings
        await DbSet.AddRangeAsync(standings, cancellationToken);

        // Save changes
        await Context.SaveChangesAsync(cancellationToken);
    }

    // Expose write operations for projections
    public new async Task UpsertAsync(GroupStandingsReadModel model, CancellationToken cancellationToken = default)
    {
        await base.UpsertAsync(model, cancellationToken);
    }

    public new async Task UpsertManyAsync(IEnumerable<GroupStandingsReadModel> models, CancellationToken cancellationToken = default)
    {
        await base.UpsertManyAsync(models, cancellationToken);
    }

    public new async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await base.DeleteAsync(id, cancellationToken);
    }

    public new async Task DeleteManyAsync(System.Linq.Expressions.Expression<Func<GroupStandingsReadModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await base.DeleteManyAsync(predicate, cancellationToken);
    }
}

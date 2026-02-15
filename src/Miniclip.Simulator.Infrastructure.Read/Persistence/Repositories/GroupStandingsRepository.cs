using Microsoft.EntityFrameworkCore;
using Miniclip.Core.EF;
using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Repositories;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories;

public class GroupStandingsRepository(SimulatorReadDbContext context) 
    : ReadModelRepository<GroupStandingsModel>(context), IGroupStandingsRepository
{
    public async Task<IEnumerable<GroupStandingsModel>> GetStandingsByGroupIdAsync(
        Guid groupId, 
        CancellationToken cancellationToken = default)
    {
        return await Context
            .Set<GroupStandingsModel>()
            .AsNoTracking()
            .Where(x => x.GroupId == groupId)
            .OrderBy(x => x.Position)
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupStandingsModel?> GetTeamStandingAsync(
        Guid groupId, 
        Guid teamId, 
        CancellationToken cancellationToken = default)
    {
        return await Context
            .Set<GroupStandingsModel>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.TeamId == teamId, cancellationToken);
    }

    public async Task<IEnumerable<GroupStandingsModel>> GetQualifiedTeamsAsync(
        Guid groupId, 
        CancellationToken cancellationToken = default)
    {
        return await Context
            .Set<GroupStandingsModel>()
            .AsNoTracking()
            .Where(x => x.GroupId == groupId && x.QualifiesForKnockout)
            .OrderBy(x => x.Position)
            .ToListAsync(cancellationToken);
    }
}

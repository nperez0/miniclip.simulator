using Miniclip.Simulator.ReadModels.Repositories.Read;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Read;

public class GroupStandingsRepository(SimulatorReadDbContext context) 
    : ReadOnlyRepository<GroupStandingsModel>(context), IGroupStandingsRepository
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
}

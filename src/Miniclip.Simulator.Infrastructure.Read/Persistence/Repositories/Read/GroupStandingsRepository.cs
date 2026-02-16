using Microsoft.EntityFrameworkCore;
using Miniclip.Core.EF;
using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Repositories.Read;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Read;

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
}

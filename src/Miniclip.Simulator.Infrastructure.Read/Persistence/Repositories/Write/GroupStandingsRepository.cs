using Microsoft.EntityFrameworkCore;
using Miniclip.Core.EF;
using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Write;

public class GroupStandingsRepository(SimulatorReadDbContext context) 
    : Repository<GroupStandingsModel>(context), IGroupStandingsRepository
{
    public async Task<IEnumerable<GroupStandingsModel>> GetStandingsByGroupIdAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var standingsFromDb = await context
            .Set<GroupStandingsModel>()
            .Where(x => x.GroupId == groupId)
            .ToArrayAsync(cancellationToken);

        var standingsInMemory = context
            .ChangeTracker
            .Entries<GroupStandingsModel>()
            .Where(e => e.State == EntityState.Added && e.Entity.GroupId == groupId)
            .Select(e => e.Entity)
            .ToArray();

        return [.. standingsFromDb, .. standingsInMemory];
    }
}

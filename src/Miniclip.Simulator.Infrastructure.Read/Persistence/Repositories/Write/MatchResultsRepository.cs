using Microsoft.EntityFrameworkCore;
using Miniclip.Core.EF;
using Miniclip.Simulator.Infrastructure.Read.Persistence;
using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Repositories.Write;
using System.Linq;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Write;

public class MatchResultsRepository(SimulatorReadDbContext context)
    : Repository<MatchResultModel>(context), IMatchResultsRepository
{
    public async Task<IEnumerable<MatchResultModel>> GetMatchResultsByGroupIdAsync(
        Guid groupId, 
        CancellationToken cancellationToken)
    {
        var matchResultsFromDb = await context
            .Set<MatchResultModel>()
            .Where(m => m.GroupId == groupId)
            .ToArrayAsync(cancellationToken);

        var matchResultsInMemory = context
            .ChangeTracker
            .Entries<MatchResultModel>()
            .Where(e => e.State == EntityState.Added && e.Entity.GroupId == groupId)
            .Select(e => e.Entity)
            .ToArray();

        return [.. matchResultsFromDb, .. matchResultsInMemory];
    }
}

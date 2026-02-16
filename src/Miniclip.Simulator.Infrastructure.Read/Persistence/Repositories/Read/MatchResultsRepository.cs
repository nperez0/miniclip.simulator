using Microsoft.EntityFrameworkCore;
using Miniclip.Core.EF;
using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Repositories.Read;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Read;

public class MatchResultsRepository(SimulatorReadDbContext context) 
    : ReadModelRepository<MatchResultModel>(context), IMatchResultsRepository
{
    public async Task<IEnumerable<MatchResultModel>> GetMatchResultsByGroupIdAsync(
        Guid groupId, 
        CancellationToken cancellationToken)
    {
        return await Context
            .Set<MatchResultModel>()
            .AsNoTracking()
            .Where(m => m.GroupId == groupId)
            .OrderBy(m => m.Round)
            .ThenBy(m => m.HomeTeamName)
            .ToListAsync(cancellationToken);
    }
}

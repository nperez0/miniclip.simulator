using Microsoft.EntityFrameworkCore;
using Miniclip.Core.EF;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.Infrastructure.Write.Persistence.Repositories;

public class GroupsRepository(SimulatorWriteDbContext context) : Repository<Group>(context)
{
    public override async Task<Group?> FindAsync(Guid id, CancellationToken cancellationToken)
        => await context.Set<Group>()
            .Include(g => g.Matches)
                .ThenInclude(m => m.HomeTeam)
            .Include(g => g.Matches)
                .ThenInclude(m => m.AwayTeam)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
}

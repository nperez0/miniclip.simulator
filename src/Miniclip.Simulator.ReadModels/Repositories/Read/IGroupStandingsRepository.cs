using Miniclip.Core.ReadModels;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Repositories.Read;

public interface IGroupStandingsRepository : IReadOnlyRepository<GroupStandingsModel>
{
    Task<IEnumerable<GroupStandingsModel>> GetStandingsByGroupIdAsync(
        Guid groupId, 
        CancellationToken cancellationToken);
}

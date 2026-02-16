using Miniclip.Core.Domain;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Repositories.Write;

public interface IGroupStandingsRepository : IRepository<GroupStandingsModel>
{
    Task<IEnumerable<GroupStandingsModel>> GetStandingsByGroupIdAsync(
        Guid groupId,
        CancellationToken cancellationToken);
}

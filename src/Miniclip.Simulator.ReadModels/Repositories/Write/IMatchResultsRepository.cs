using Miniclip.Core.Domain;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Repositories.Write;

public interface IMatchResultsRepository : IRepository<MatchResultModel>
{
    Task<IEnumerable<MatchResultModel>> GetMatchResultsByGroupIdAsync(
        Guid groupId,
        CancellationToken cancellationToken);
}

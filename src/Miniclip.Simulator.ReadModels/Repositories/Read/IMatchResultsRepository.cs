using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Repositories.Read;

public interface IMatchResultsRepository
{
    Task<IEnumerable<MatchResultModel>> GetMatchResultsByGroupIdAsync(
        Guid groupId, 
        CancellationToken cancellationToken);
}

using Miniclip.Core.ReadModels;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Repositories;

public interface IMatchResultRepository : IReadOnlyRepository<MatchResultReadModel>
{
    Task<IEnumerable<MatchResultReadModel>> GetMatchesByGroupIdAsync(
        Guid groupId, 
        CancellationToken cancellationToken);
    
    Task<IEnumerable<MatchResultReadModel>> GetMatchesByRoundAsync(
        Guid groupId, 
        int round, 
        CancellationToken cancellationToken);
    
    Task<IEnumerable<MatchResultReadModel>> GetMatchesByTeamIdAsync(
        Guid teamId, 
        CancellationToken cancellationToken);
    
    Task RebuildMatchResultsAsync(
        Guid groupId, 
        IEnumerable<MatchResultReadModel> matchResults, 
        CancellationToken cancellationToken = default);
}

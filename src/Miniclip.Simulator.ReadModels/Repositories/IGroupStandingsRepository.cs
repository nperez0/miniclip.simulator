using Miniclip.Core.ReadModels;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Repositories;

public interface IGroupStandingsRepository : IReadOnlyRepository<GroupStandingsReadModel>
{
    Task<IEnumerable<GroupStandingsReadModel>> GetStandingsByGroupIdAsync(
        Guid groupId, 
        CancellationToken cancellationToken);
    
    Task<GroupStandingsReadModel?> GetTeamStandingAsync(
        Guid groupId, 
        Guid teamId, 
        CancellationToken cancellationToken);
    
    Task<IEnumerable<GroupStandingsReadModel>> GetQualifiedTeamsAsync(
        Guid groupId, 
        CancellationToken cancellationToken);
    
    Task RebuildStandingsAsync(
        Guid groupId, 
        IEnumerable<GroupStandingsReadModel> standings, 
        CancellationToken cancellationToken);
}

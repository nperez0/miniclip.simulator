using Miniclip.Core.ReadModels;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Repositories;

public interface IGroupStandingsRepository : IReadOnlyRepository<GroupStandingsModel>
{
    Task<IEnumerable<GroupStandingsModel>> GetStandingsByGroupIdAsync(
        Guid groupId, 
        CancellationToken cancellationToken);
    
    Task<GroupStandingsModel?> GetTeamStandingAsync(
        Guid groupId, 
        Guid teamId, 
        CancellationToken cancellationToken);
    
    Task<IEnumerable<GroupStandingsModel>> GetQualifiedTeamsAsync(
        Guid groupId, 
        CancellationToken cancellationToken);
}

using Miniclip.Core.ReadModels;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Repositories;

public interface IGroupOverviewRepository : IReadOnlyRepository<GroupOverviewReadModel>
{
    Task<GroupOverviewReadModel?> GetOverviewByGroupIdAsync(
        Guid groupId, 
        CancellationToken cancellationToken);
    
    Task<IEnumerable<GroupOverviewReadModel>> GetAllOverviewsAsync(
        CancellationToken cancellationToken);
    
    Task UpsertAsync(GroupOverviewReadModel model, CancellationToken cancellationToken = default);
}

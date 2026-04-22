
namespace Miniclip.Simulator.ReadModels.Repositories.Write;

public interface IGroupStandingsRepository : IRepository<GroupStandingsModel>
{
    Task<IEnumerable<GroupStandingsModel>> GetStandingsByGroupIdAsync(
        Guid groupId,
        CancellationToken cancellationToken);
}

using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Projections.Services;

public interface IRecalculatePositionService
{
    Task RecalculatePositionsAsync(IEnumerable<GroupStandingsModel> standings, Guid groupId, CancellationToken cancellationToken);
}
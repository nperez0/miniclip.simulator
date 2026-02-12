using MediatR;
using Miniclip.Simulator.ReadModels.Repositories;

namespace Miniclip.Simulator.Application.Queries.Groups.V1.Standings;

/// <summary>
/// Handles queries for group standings.
/// Reads from pre-calculated read models - blazing fast!
/// </summary>
public class GroupStandingsQueryHandler(IGroupStandingsRepository repository) 
    : IRequestHandler<GroupStandingsQuery, GroupStandingsDto>
{
    public async Task<GroupStandingsDto> Handle(GroupStandingsQuery query, CancellationToken cancellationToken)
    {
        var standings = await repository.GetStandingsByGroupIdAsync(query.GroupId, cancellationToken);
        
        var standingsList = standings.ToList();
        
        if (!standingsList.Any())
            throw new Exception($"No standings found for group {query.GroupId}");

        return new GroupStandingsDto
        {
            GroupId = query.GroupId,
            GroupName = standingsList.First().GroupName,
            Standings = standingsList
        };
    }
}

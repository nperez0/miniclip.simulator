using MediatR;
using Miniclip.Simulator.ReadModels.Repositories;

namespace Miniclip.Simulator.Application.Queries.Groups.V1.MatchResults;

public class MatchResultsQueryHandler(IMatchResultRepository repository)  
    : IRequestHandler<MatchResultsQuery, MatchResultsDto>
{
    public async Task<MatchResultsDto> Handle(MatchResultsQuery query, CancellationToken cancellationToken)
    {
        var matches = await repository.GetMatchesByGroupIdAsync(query.GroupId, cancellationToken);
        
        var matchList = matches.ToList();
        
        if (!matchList.Any())
            throw new Exception($"No matches found for group {query.GroupId}");

        return new MatchResultsDto
        {
            GroupId = query.GroupId,
            GroupName = matchList.First().GroupName,
            Matches = matchList
        };
    }
}

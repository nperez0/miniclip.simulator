using MediatR;
using Miniclip.Simulator.ReadModels.Repositories.Read;

namespace Miniclip.Simulator.Application.Queries.Groups.V1.Standings;

public class GroupStandingsQueryHandler(
    IGroupStandingsRepository standingsRepository,
    IMatchResultsRepository matchResultRepository) 
    : IRequestHandler<GroupStandingsQuery, GroupStandingsDto>
{
    public async Task<GroupStandingsDto> Handle(GroupStandingsQuery query, CancellationToken cancellationToken)
    {
        var standings = await standingsRepository.GetStandingsByGroupIdAsync(query.GroupId, cancellationToken);
        var matchResults = await matchResultRepository.GetMatchResultsByGroupIdAsync(query.GroupId, cancellationToken);

        var standingsList = standings.ToArray();

        if (standingsList.Length == 0)
            return new GroupStandingsDto();

        return new GroupStandingsDto
        {
            GroupId = query.GroupId,
            GroupName = standingsList.First().GroupName,
            Standings = standingsList.Select(s => new TeamStandingDto
            {
                Position = s.Position,
                TeamId = s.TeamId,
                TeamName = s.TeamName,
                TeamStrength = s.TeamStrength,
                MatchesPlayed = s.MatchesPlayed,
                Wins = s.Wins,
                Draws = s.Draws,
                Losses = s.Losses,
                GoalsFor = s.GoalsFor,
                GoalsAgainst = s.GoalsAgainst,
                GoalDifference = s.GoalDifference,
                Points = s.Points,
                QualifiesForKnockout = s.QualifiesForKnockout
            })
            .ToArray(),
            MatchResults = matchResults.Select(m => new MatchResultDto
            {
                MatchId = m.MatchId,
                Round = m.Round,
                HomeTeamName = m.HomeTeamName,
                HomeScore = m.HomeScore,
                AwayTeamName = m.AwayTeamName,
                AwayScore = m.AwayScore,
                PlayedAt = m.PlayedAt
            })
            .ToArray()
        };
    }
}

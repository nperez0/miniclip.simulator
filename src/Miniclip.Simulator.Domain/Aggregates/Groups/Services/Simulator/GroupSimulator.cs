using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

public class GroupSimulator(IMatchSimulator matchSimulator)
{
    public Result SimulateAllMatches(Group group)
    {
        var matchesNotPlayed = group.Matches.Where(m => !m.IsPlayed);

        foreach (var match in matchesNotPlayed)
        {
            var result = SimulateMatch(match);

            if (result.IsFailure)
                return result;
        }

        return Result.Success();
    }

    private Result SimulateMatch(Match match)
    {
        var (homeScore, awayScore) = matchSimulator.SimulateMatch(match.HomeTeam.Strength, match.AwayTeam.Strength);

        return match.SimulateResult(homeScore, awayScore);
    }
}

using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Errors;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

public class GroupSimulator(IMatchSimulatorFactory matchSimulatorFactory) : IGroupSimulator
{
    public Result SimulateAllMatches(Group group)
    {
        var matchSimulator = matchSimulatorFactory.Create(group);
        var matchesNotPlayed = group
            .Matches
            .Where(m => !m.IsPlayed)
            .ToArray();

        if (matchesNotPlayed.Length == 0)
            return Result.Failure(GroupSimulationErrors.AllMatchesPlayed(group.Id));

        return matchesNotPlayed
            .Traverse(match => SimulateMatch(group, match, matchSimulator));
    }

    private static Result SimulateMatch(Group group, Match match, IMatchSimulator matchSimulator)
    {
        var (homeScore, awayScore) = matchSimulator.SimulateMatch(match.HomeTeam.Strength, match.AwayTeam.Strength);

        return group.SimulateMatch(match.Id, homeScore, awayScore);
    }
}

using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

public class GroupSimulator : IGroupSimulator
{
    private readonly IMatchSimulatorFactory IMatchSimulatorFactory;

    public GroupSimulator(IMatchSimulatorFactory IMatchSimulatorFactory)
    {
        this.IMatchSimulatorFactory = IMatchSimulatorFactory;
    }

    public Result SimulateAllMatches(Group group)
    {
        var matchSimulator = IMatchSimulatorFactory.Create(group);
        var matchesNotPlayed = group
            .Matches
            .Where(m => !m.IsPlayed)
            .ToArray();

        if (matchesNotPlayed.Length == 0)
            return Result.Failure(GroupSimulationErrors.AllMatchesPlayed());

        return matchesNotPlayed
            .Traverse(match => SimulateMatch(group, match, matchSimulator));
    }

    private static Result SimulateMatch(Group group, Match match, IMatchSimulator matchSimulator)
    {
        var (homeScore, awayScore) = matchSimulator.SimulateMatch(match.HomeTeam.Strength, match.AwayTeam.Strength);

        return group.SimulateMatch(match.Id, homeScore, awayScore);
    }
}

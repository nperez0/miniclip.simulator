namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public static class GroupGenerateFixturesErrors
{
    public static Error InvalidTeamCount(int capacity, int count)
        => Error.Validation("GROUP_INVALID_TEAM_COUNT", $"Group must have exactly {capacity} teams to generate fixtures. Current count: {count}.");

    public static Error SameTeam()
        => Error.Validation("GROUP_SAME_TEAM", "A team cannot play against itself.");
}

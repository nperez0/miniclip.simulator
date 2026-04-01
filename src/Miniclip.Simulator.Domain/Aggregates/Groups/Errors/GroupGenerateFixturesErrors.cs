namespace Miniclip.Simulator.Domain.Aggregates.Groups.Errors;

public static class GroupGenerateFixturesErrors
{
    public const string InvalidTeamCountCode = "GROUP_INVALID_TEAM_COUNT";
    public const string SameTeamCode = "GROUP_SAME_TEAM";

    public static Error InvalidTeamCount(int capacity, int count)
        => Error.Conflict(InvalidTeamCountCode, $"Group must have exactly {capacity} teams to generate fixtures. Current count: {count}.");
    public static Error SameTeam(Guid teamId)
        => Error.Conflict(SameTeamCode, $"A team cannot play against itself. Team ID: {teamId}");
}

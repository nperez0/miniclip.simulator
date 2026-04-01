namespace Miniclip.Simulator.Domain.Aggregates.Groups.Errors;

public static class GroupAddTeamErrors
{
    public const string MaxTeamsReachedCode = "GROUP_MAX_TEAMS_REACHED";
    public const string TeamAlreadyExistsCode = "GROUP_TEAM_ALREADY_EXISTS";

    public static Error MaxTeamsReached(int capacity)
        => Error.Conflict(MaxTeamsReachedCode, $"Has reached the maximum number of teams: {capacity}.");

    public static Error TeamAlreadyExists(Guid teamId)
        => Error.Conflict(TeamAlreadyExistsCode, $"Team '{teamId}' already exists in the group.");
}

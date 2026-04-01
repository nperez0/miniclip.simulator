namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public static class GroupAddTeamErrors
{
    public static Error MaxTeamsReached(int capacity)
        => Error.Validation("GROUP_MAX_TEAMS_REACHED", $"Has reached the maximum number of teams: {capacity}.");

    public static Error TeamAlreadyExists(Guid teamId)
        => Error.Conflict("GROUP_TEAM_ALREADY_EXISTS", $"Team '{teamId}' already exists in the group.");
}

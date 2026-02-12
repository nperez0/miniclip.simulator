namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public class GroupAddTeamException(string message) : Exception(message)
{
    public static GroupAddTeamException MaxTeamsReached(int capacity)
        => new($"Has reached the maximum number of teams: {capacity}.");

    public static GroupAddTeamException TeamAlreadyExists(Guid teamId)
        => new($"Team '{teamId}' already exists in the group.");
}

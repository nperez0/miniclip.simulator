namespace Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;

public static class SimulateGroupErrors
{
    public const string GroupNotFoundCode = "GROUP_NOT_FOUND";

    public static Error GroupNotFound(Guid groupId)
        => Error.NotFound(GroupNotFoundCode, $"Group {groupId} not found");
}

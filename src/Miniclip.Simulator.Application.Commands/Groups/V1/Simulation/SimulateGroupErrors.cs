namespace Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;

public static class SimulateGroupErrors
{
    public static Error GroupNotFound(Guid groupId)
        => Error.NotFound("GROUP_NOT_FOUND", $"Group {groupId} not found");
}

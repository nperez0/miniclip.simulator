namespace Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;

public class SimulateGroupException(string message, ExceptionType type = ExceptionType.General) : ExceptionBase(message, type)
{
    public static SimulateGroupException GroupNotFound(Guid groupId)
        => new($"Group {groupId} not found", ExceptionType.NotFound);
}

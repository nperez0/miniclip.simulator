using Miniclip.Core.Domain.Exceptions;

namespace Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;

public class GroupNotFoundException(string message) : NotFoundException(message)
{
    public static GroupNotFoundException NotFound(Guid groupId)
        => new($"Group {groupId} not found");
}

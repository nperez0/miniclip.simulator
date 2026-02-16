using Miniclip.Core.Domain.Exceptions;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public class MatchNotFoundException(string message) : NotFoundException(message)
{
    public static MatchNotFoundException NotFound(Guid matchId)
        => new($"Match with ID '{matchId}' not found.");
}

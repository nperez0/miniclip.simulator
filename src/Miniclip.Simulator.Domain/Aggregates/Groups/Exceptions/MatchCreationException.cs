namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public class MatchCreationException(string message) : Exception(message)
{
    public static MatchCreationException SameTeam()
        => new("A team cannot play against itself.");

}
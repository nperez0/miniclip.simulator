using Miniclip.Core;
using Miniclip.Simulator.Domain.Exceptions;

namespace Miniclip.Simulator.Domain.Entities;

public class Team
{
    public int Id { get; }
    public string Name { get; }
    public int Strength { get; } // 0-100: influences match outcomes

    private Team(int id, string name, int strength)
    {
        Id = id;
        Name = name;
        Strength = strength;
    }

    public static Result<Team> Create(int id, string? name, int strength)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Team>(TeamCreationException.EmptyName(name));
        if (strength < 0 || strength > 100)
            return Result.Failure<Team>(TeamCreationException.InvalidStrength(strength));

        return Result.Success(new Team(id, name, strength));
    }
}

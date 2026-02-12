using Miniclip.Core;
using Miniclip.Core.Domain;
using Miniclip.Simulator.Domain.Aggregates.Teams.Events;
using Miniclip.Simulator.Domain.Aggregates.Teams.Exceptions;

namespace Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

public class Team : AggregateRoot
{
    public static Team Dummy { get; } = new(new Guid(), "Dummy", 1);

    public string Name { get; }
    public int Strength { get; } // 0-100: influences match outcomes

    private Team(Guid id, string name, int strength)
    {
        Id = id;
        Name = name;
        Strength = strength;

        Enqueue(new TeamCreated(Id, Name, Strength));
    }

    public static Result<Team> Create(Guid id, string? name, int strength)
    {
        if (name.IsNullOrWhiteSpace())
            return Result.Failure<Team>(TeamCreationException.EmptyName(name));
        if (strength < 0 || strength > 100)
            return Result.Failure<Team>(TeamCreationException.InvalidStrength(strength));

        return new Team(id, name, strength);
    }
}

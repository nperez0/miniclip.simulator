using Miniclip.Core.Domain;
using Miniclip.Simulator.Domain.Aggregates.Teams.Events;
using Miniclip.Simulator.Domain.Aggregates.Teams.Validations;

namespace Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

public class Team : AggregateRoot
{
    public string Name { get; private set; } = null!;
    public int Strength { get; private set; } // 0-100: influences match outcomes

    private Team()
    {
    }

    private Team(Guid id, string name, int strength)
    {
        Id = id;
        Name = name;
        Strength = strength;
    }

    public static Result<Team> Create(Guid id, string? name, int strength)
        => Validation.For<Team>()
            .HasValidName(name)
            .HasValidStrength(strength)
            .Validate()
            .Map(() =>
            {
                var team = new Team(id, name!, strength);
                team.Enqueue(new TeamRegistered(id, name!, strength));
                return team;
            });

    protected override void Apply(IDomainEvent @event)
    {
        if (@event is not TeamRegistered e) 
            return;

        Id = e.TeamId;
        Name = e.Name;
        Strength = e.Strength;
    }
}

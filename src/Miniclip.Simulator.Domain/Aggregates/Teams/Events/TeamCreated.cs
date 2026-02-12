namespace Miniclip.Simulator.Domain.Aggregates.Teams.Events;

public class TeamCreated(Guid Id, string Name, int Strength)
{
    public Guid Id { get; } = Id;
    public string Name { get; } = Name;
    public int Strength { get; } = Strength;
}

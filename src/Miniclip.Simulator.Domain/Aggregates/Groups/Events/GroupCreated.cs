using Miniclip.Core.Domain;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Events;

public class GroupCreated : IDomainEvent
{
    public Guid GroupId { get; }
    public string Name { get; }
    public int Capacity { get; }

    public GroupCreated(Guid groupId, string name, int capacity)
    {
        GroupId = groupId;
        Name = name;
        Capacity = capacity;
    }
}

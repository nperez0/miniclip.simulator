using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;

public class FixtureSchedulerFactory : IFixtureSchedulerFactory
{
    public IFixtureScheduler Create(Group group)
        // In a real-world scenario, we could use a configuration or Group properties
        // to determine which implementation of IFixtureScheduler to create.
        => new RoundRobinScheduler(group.Teams, group.Capacity);
}

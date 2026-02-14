using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;

public interface IFixtureSchedulerFactory
{
    IFixtureScheduler Create(Group group);
}
using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;

public interface IFixtureSchedulerService
{
    Result GenerateFixtures(Group group);
}
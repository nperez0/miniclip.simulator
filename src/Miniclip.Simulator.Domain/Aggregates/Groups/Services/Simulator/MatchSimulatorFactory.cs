using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

public class MatchSimulatorFactory : IMatchSimulatorFactory
{
    public IMatchSimulator Create(Group group)
        // In a real-world scenario, we could use a configuration or Group properties
        // to determine which implementation of IMatchSimulator to create.
        => new MatchSimulator();
}

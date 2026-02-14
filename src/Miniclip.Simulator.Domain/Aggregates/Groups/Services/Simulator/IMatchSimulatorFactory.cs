using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

public interface IMatchSimulatorFactory
{
    IMatchSimulator Create(Group group);
}
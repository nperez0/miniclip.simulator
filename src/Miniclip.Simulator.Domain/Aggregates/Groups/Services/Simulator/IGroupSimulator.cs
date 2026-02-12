using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

public interface IGroupSimulator
{
    Result SimulateAllMatches(Group group);
}
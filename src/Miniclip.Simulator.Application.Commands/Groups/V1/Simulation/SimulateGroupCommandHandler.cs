using Mediator;
using Miniclip.Core.Domain;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

namespace Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;

public class SimulateGroupCommandHandler(
    IAggregateRepository<Group> repository,
    IGroupSimulator groupSimulator) 
    : IRequestHandler<SimulateGroupCommand, Result>
{
    public async ValueTask<Result> Handle(SimulateGroupCommand command, CancellationToken cancellationToken)
    {
        var group = await repository.FindAsync(command.GroupId, cancellationToken);
        
        if (group == null)
            return Result.Failure(SimulateGroupException.GroupNotFound(command.GroupId));

        return groupSimulator.SimulateAllMatches(group);
    }
}

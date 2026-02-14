using MediatR;
using Miniclip.Core;
using Miniclip.Core.Domain;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

namespace Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;

public class SimulateGroupCommandHandler(
    IRepository<Group> repository,
    IGroupSimulator groupSimulator) 
    : IRequestHandler<SimulateGroupCommand, Result>
{
    public async Task<Result> Handle(SimulateGroupCommand command, CancellationToken cancellationToken)
    {
        var group = await repository.FindAsync(command.GroupId, cancellationToken);
        
        if (group == null)
            return Result.Failure(GroupNotFoundException.NotFound(command.GroupId));

        return groupSimulator.SimulateAllMatches(group);
    }
}

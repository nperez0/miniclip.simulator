using MediatR;
using Miniclip.Core.Domain;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Projections;

public class GroupStandingsProjection(IRepository<GroupStandingsModel> repository) : INotificationHandler<GroupCreated>
{
    public Task Handle(GroupCreated notification, CancellationToken cancellationToken)
    {
        var groupStandings = new GroupStandingsModel
        {
            Id = Guid.NewGuid(),
            GroupId = notification.GroupId,
            GroupName = notification.Name
        };

        repository.Add(groupStandings);

        return Task.CompletedTask;
    }
}

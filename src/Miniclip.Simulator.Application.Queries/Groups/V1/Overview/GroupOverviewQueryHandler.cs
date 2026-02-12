using MediatR;
using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Repositories;

namespace Miniclip.Simulator.Application.Queries.Groups.V1.Overview;

public class GroupOverviewQueryHandler(IGroupOverviewRepository repository) 
    : IRequestHandler<GroupOverviewQuery, GroupOverviewReadModel>
{
    public async Task<GroupOverviewReadModel> Handle(GroupOverviewQuery request, CancellationToken cancellationToken)
    {
        var overview = await repository.GetOverviewByGroupIdAsync(request.GroupId, cancellationToken);

        if (overview == null)
            throw new Exception($"Group overview not found for group {request.GroupId}");

        return overview;
    }
}

using MediatR;
using Miniclip.Core.Application;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.Application.Queries.Groups.V1.Overview;

public record GroupOverviewQuery(Guid GroupId) : IQuery<GroupOverviewReadModel>;

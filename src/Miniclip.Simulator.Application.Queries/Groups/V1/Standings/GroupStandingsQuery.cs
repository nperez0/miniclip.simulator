using Miniclip.Core.Application;

namespace Miniclip.Simulator.Application.Queries.Groups.V1.Standings;

public record GroupStandingsQuery(Guid GroupId) : IQuery<GroupStandingsDto>;

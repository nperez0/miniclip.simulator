using Miniclip.Core.Application;

namespace Miniclip.Simulator.Application.Queries.Groups.V1.MatchResults;

public record MatchResultsQuery(Guid GroupId) : IQuery<MatchResultsDto>;

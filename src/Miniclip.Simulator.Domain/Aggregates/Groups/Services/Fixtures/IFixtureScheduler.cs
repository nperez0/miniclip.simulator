using Miniclip.Simulator.Domain.Aggregates.Groups.ValueObjects;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;

public interface IFixtureScheduler
{
    IEnumerable<(TeamInfo HomeTeam, TeamInfo AwayTeam, int Round)> GenerateSchedule();
}

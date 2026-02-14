using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;

public interface IFixtureScheduler
{
    IEnumerable<(Team HomeTeam, Team AwayTeam, int Round)> GenerateSchedule();
}
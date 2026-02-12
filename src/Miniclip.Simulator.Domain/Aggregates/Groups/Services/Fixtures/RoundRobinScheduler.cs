using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;

internal static class RoundRobinScheduler
{
    public static IEnumerable<(Team HomeTeam, Team AwayTeam, int Round)> GenerateSchedule(IReadOnlyCollection<Team> teams, int capacity)
    {
        var isOdd = capacity % 2 == 1;

        var rotatedTeams = teams.ToList();

        // For odd number of teams, add a dummy (null) to make pairs work
        if (isOdd)
            rotatedTeams.Add(Team.Dummy);

        // For odd teams, we need as many rounds as teams (each team gets one bye)
        // For even teams, we need capacity - 1 rounds
        var numberOfRounds = isOdd ? capacity : capacity - 1;
        var teamsCount = rotatedTeams.Count;
        var fixturesPerRound = teamsCount / 2;
        var currentFixtureIndex = 0;
        var currentRound = 1;

        do
        {
            var homeTeam = rotatedTeams[currentFixtureIndex];
            var awayTeam = rotatedTeams[teamsCount - 1 - currentFixtureIndex];

            // Skip matches involving the dummy team
            if (homeTeam != Team.Dummy && awayTeam != Team.Dummy)
                yield return (homeTeam, awayTeam, currentRound);

            currentFixtureIndex++;

            var hasReachedFixturesPerRound = currentFixtureIndex == fixturesPerRound;

            if (hasReachedFixturesPerRound)
            {
                var hasReachedMaxRounds = currentRound == numberOfRounds;

                if (hasReachedMaxRounds)
                    break;

                currentFixtureIndex = 0;
                currentRound++;

                // Rotate teams (keep first team fixed)
                var lastTeam = rotatedTeams.Last();
                rotatedTeams.Remove(lastTeam);
                rotatedTeams.Insert(1, lastTeam);
            }

        } while (true);
    }
}

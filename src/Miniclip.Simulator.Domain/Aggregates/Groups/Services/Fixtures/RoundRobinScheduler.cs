using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;

public class RoundRobinScheduler : IFixtureScheduler
{
    private readonly bool isOdd;
    private readonly List<Team> rotatedTeams;
    private readonly int numberOfRounds;
    private readonly int teamsCount;
    private readonly int fixturesPerRound;
    private readonly Dictionary<Team, int> homeCounter;
    private readonly Dictionary<Team, int> awayCounter;

    public RoundRobinScheduler(IReadOnlyCollection<Team> teams, int capacity)
    {
        isOdd = capacity % 2 == 1;
        rotatedTeams = [.. teams];

        // For odd number of teams, add a dummy (null) to make pairs work
        if (isOdd)
            rotatedTeams.Add(Team.Dummy);

        // For odd teams, we need as many rounds as teams (each team gets one bye)
        // For even teams, we need capacity - 1 rounds
        numberOfRounds = isOdd ? capacity : capacity - 1;
        teamsCount = rotatedTeams.Count;
        fixturesPerRound = teamsCount / 2;
        homeCounter = teams.ToDictionary(t => t, _ => 0);
        awayCounter = teams.ToDictionary(t => t, _ => 0);
    }

    public IEnumerable<(Team HomeTeam, Team AwayTeam, int Round)> GenerateSchedule()
    {
        if (teamsCount <= 1 || fixturesPerRound == 0)
            yield break;

        var currentRound = 1;
        var currentFixtureIndex = 0;

        do
        {
            var firstTeam = rotatedTeams[currentFixtureIndex];
            var secondTeam = rotatedTeams[teamsCount - 1 - currentFixtureIndex];
            
            // Skip matches involving the dummy team
            if (firstTeam != Team.Dummy && secondTeam != Team.Dummy)
            {
                var (homeTeam, awayTeam) = GetMatchup(firstTeam, secondTeam);

                yield return (homeTeam, awayTeam, currentRound);
            }   

            currentFixtureIndex++;

            var hasReachedFixturesPerRound = currentFixtureIndex == fixturesPerRound;

            if (hasReachedFixturesPerRound)
            {
                var hasReachedMaxRounds = currentRound == numberOfRounds;

                if (hasReachedMaxRounds)
                    break;

                currentRound++;
                currentFixtureIndex = 0;
                
                RoteateTeams();
            }

        } while (true);
    }

    private (Team HomeTeam, Team AwayTeam) GetMatchup(Team firstTeam, Team secondTeam)
    {
        // This ensures teams get a mix of home and away games
        var shouldSwap = homeCounter[firstTeam] > homeCounter[secondTeam]
            || awayCounter[secondTeam] > awayCounter[firstTeam];

        var (homeTeam, awayTeam) = shouldSwap
            ? (secondTeam, firstTeam)
            : (firstTeam, secondTeam);

        homeCounter[homeTeam]++;
        awayCounter[awayTeam]++;

        return (homeTeam, awayTeam);
    }

    private void RoteateTeams()
    {
        // Rotate teams (keep first team fixed)
        var lastTeam = rotatedTeams.Last();
        rotatedTeams.Remove(lastTeam);
        rotatedTeams.Insert(1, lastTeam);
    }
}

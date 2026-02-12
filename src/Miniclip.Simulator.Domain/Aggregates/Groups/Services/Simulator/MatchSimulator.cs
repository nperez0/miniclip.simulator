namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

public class MatchSimulator : IMatchSimulator
{
    private const double HomeAdvantage = 1.1;
    private const double BaseExpectedGoals = 3.5; // Base expected goals between 0-4
    private const int MaxGoals = 10;

    private readonly Random random = Random.Shared;

    public (int homeScore, int awayScore) SimulateMatch(int homeTeamStrength, int awayTeamStrength)
    {
        // Home advantage: slight boost to home team
        var adjustedHomeStrength = homeTeamStrength * HomeAdvantage;

        // Generate goals using Poisson-like distribution
        var homeScore = GenerateGoals(adjustedHomeStrength);
        var awayScore = GenerateGoals(awayTeamStrength);

        return (homeScore, awayScore);
    }

    private int GenerateGoals(double Strength)
    {
        // Calculate scoring probabilities based on strength
        var attackPower = Strength / 100.0;
        var expectedGoals = attackPower * BaseExpectedGoals;

        // Simplified Poisson: generate goals with some randomness
        var cumulative = 0d;
        var nextRandom = random.NextDouble();

        for (var goals = 0; goals <= MaxGoals; goals++)
        {
            var probability = PoissonProbability(goals, expectedGoals);

            cumulative += probability;

            if (nextRandom <= cumulative)
                return goals;
        }

        return 0;
    }

    private static double PoissonProbability(int k, double lambda)
        => Math.Pow(lambda, k) * Math.Exp(-lambda) / Factorial(k);

    private static double Factorial(int n)
    {
        if (n <= 1) 
            return 1;

        var result = 1;

        for (int i = 2; i <= n; i++)
            result *= i;

        return result;
    }
}

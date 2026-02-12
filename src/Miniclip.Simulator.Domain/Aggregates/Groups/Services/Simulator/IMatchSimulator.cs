namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

public interface IMatchSimulator
{
    (int homeScore, int awayScore) SimulateMatch(int homeTeamStrength, int awayTeamStrength);
}

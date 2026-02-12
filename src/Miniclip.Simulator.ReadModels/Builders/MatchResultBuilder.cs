using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Builders;

/// <summary>
/// Builds denormalized match result read models from domain entities.
/// </summary>
public class MatchResultBuilder
{
    /// <summary>
    /// Build match result read models for all matches in a group.
    /// </summary>
    public List<MatchResultReadModel> BuildMatchResults(Group group)
    {
        return group.Matches.Select(match => BuildMatchResult(group, match)).ToList();
    }

    /// <summary>
    /// Build a single match result read model.
    /// </summary>
    public MatchResultReadModel BuildMatchResult(Group group, Match match)
    {
        var result = DetermineResult(match);
        var goalDifference = match.IsPlayed ? Math.Abs(match.HomeScore - match.AwayScore) : 0;

        return new MatchResultReadModel
        {
            Id = match.Id,
            GroupId = group.Id,
            GroupName = group.Name,
            HomeTeamId = match.HomeTeam.Id,
            HomeTeamName = match.HomeTeam.Name,
            HomeTeamStrength = match.HomeTeam.Strength,
            AwayTeamId = match.AwayTeam.Id,
            AwayTeamName = match.AwayTeam.Name,
            AwayTeamStrength = match.AwayTeam.Strength,
            HomeScore = match.HomeScore,
            AwayScore = match.AwayScore,
            Round = match.Round,
            IsPlayed = match.IsPlayed,
            PlayedAt = match.IsPlayed ? DateTime.UtcNow : null,
            Result = result,
            GoalDifference = goalDifference
        };
    }

    private string DetermineResult(Match match)
    {
        if (!match.IsPlayed)
            return "Not Played";

        if (match.HomeScore > match.AwayScore)
            return "Home Win";
        
        if (match.AwayScore > match.HomeScore)
            return "Away Win";
        
        return "Draw";
    }
}

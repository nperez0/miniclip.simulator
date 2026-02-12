using Miniclip.Core.Domain;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.ReadModels.Builders;
using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Repositories;

namespace Miniclip.Simulator.ReadModels.Projections;

/// <summary>
/// Projection that updates read models after a group simulation completes.
/// This is triggered after the SimulateGroupCommand successfully executes.
/// Implements eventual consistency - read models are updated asynchronously.
/// </summary>
public class GroupSimulatedProjection
{
    private readonly IRepository<Group> _groupRepository;
    private readonly IGroupStandingsRepository _standingsRepository;
    private readonly IMatchResultRepository _matchResultRepository;
    private readonly IGroupOverviewRepository _overviewRepository;
    private readonly GroupStandingsBuilder _standingsBuilder;
    private readonly MatchResultBuilder _matchResultBuilder;
    private readonly GroupOverviewBuilder _overviewBuilder;

    public GroupSimulatedProjection(
        IRepository<Group> groupRepository,
        IGroupStandingsRepository standingsRepository,
        IMatchResultRepository matchResultRepository,
        IGroupOverviewRepository overviewRepository,
        GroupStandingsBuilder standingsBuilder,
        MatchResultBuilder matchResultBuilder,
        GroupOverviewBuilder overviewBuilder)
    {
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _standingsRepository = standingsRepository ?? throw new ArgumentNullException(nameof(standingsRepository));
        _matchResultRepository = matchResultRepository ?? throw new ArgumentNullException(nameof(matchResultRepository));
        _overviewRepository = overviewRepository ?? throw new ArgumentNullException(nameof(overviewRepository));
        _standingsBuilder = standingsBuilder ?? throw new ArgumentNullException(nameof(standingsBuilder));
        _matchResultBuilder = matchResultBuilder ?? throw new ArgumentNullException(nameof(matchResultBuilder));
        _overviewBuilder = overviewBuilder ?? throw new ArgumentNullException(nameof(overviewBuilder));
    }

    /// <summary>
    /// Project group data to all read models.
    /// Called after group simulation completes.
    /// </summary>
    public async Task ProjectAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        // 1. Load the domain aggregate
        var group = await _groupRepository.FindAsync(groupId);
        
        if (group == null)
            throw new Exception($"Group with id {groupId} not found");

        // 2. Build denormalized read models from domain entities
        var standings = _standingsBuilder.BuildStandings(group);
        var matchResults = _matchResultBuilder.BuildMatchResults(group);
        var overview = _overviewBuilder.BuildOverview(group);

        // 3. Update all read model stores (replace old data with new)
        //await _standingsRepository.RebuildStandingsAsync(groupId, standings, cancellationToken);
        //await UpdateMatchResults(groupId, matchResults, cancellationToken);
        //await _overviewRepository.UpsertAsync(overview, cancellationToken);
    }

    private async Task UpdateMatchResults(
        Guid groupId, 
        List<MatchResultReadModel> matchResults, 
        CancellationToken cancellationToken)
    {
        // Delete old match results for this group
        //await _matchResultRepository.DeleteManyAsync(m => m.GroupId == groupId, cancellationToken);
        
        // Insert new match results
        //await _matchResultRepository.UpsertManyAsync(matchResults, cancellationToken);
    }
}

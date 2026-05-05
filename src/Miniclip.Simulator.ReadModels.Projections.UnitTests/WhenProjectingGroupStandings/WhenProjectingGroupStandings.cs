using Miniclip.Simulator.IntegrationEvents.V1;
using Miniclip.Simulator.ReadModels.Projections.Services;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenProjectingGroupStandings;

public abstract class WhenProjectingGroupStandings : AsyncTestBase<GroupStandingsProjection>
{
    protected IGroupStandingsRepository Repository { get; private set; } = null!;
    protected IRecalculatePositionService RecalculatePositionService { get; private set; } = null!;
    protected MatchPlayedIntegrationEvent Event { get; set; } = null!;

    protected override Task GivenAsync()
    {
        Repository = Substitute.For<IGroupStandingsRepository>();
        RecalculatePositionService = Substitute.For<IRecalculatePositionService>();

        return SetupScenarioAsync();
    }

    protected override GroupStandingsProjection CreateSystemUnderTest()
        => new(Repository, RecalculatePositionService);

    protected override async Task WhenAsync()
    {
        await Sut!.HandleAsync(Event, CancellationToken.None);
    }
}

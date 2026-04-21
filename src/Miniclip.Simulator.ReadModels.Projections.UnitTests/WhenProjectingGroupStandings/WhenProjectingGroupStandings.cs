using AutoFixture;
using Miniclip.Core.Tests;
using Miniclip.Simulator.IntegrationEvents.V1;
using Miniclip.Simulator.ReadModels.Projections.Services;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenProjectingGroupStandings;

public abstract class WhenProjectingGroupStandings : TestBase<GroupStandingsProjection>
{
    protected IGroupStandingsRepository Repository { get; private set; } = null!;
    protected IRecalculatePositionService RecalculatePositionService { get; private set; } = null!;
    protected MatchPlayedIntegrationEvent Event { get; set; } = null!;

    override protected void Given()
    {
        Repository = Fixture.Freeze<IGroupStandingsRepository>();
        RecalculatePositionService = Fixture.Freeze<IRecalculatePositionService>();
    }

    protected virtual void SetupRepositoryMock() { }

    protected override void When()
    {
        Sut!.HandleAsync(Event, CancellationToken.None).AsTask().Wait();
    }
}

using AutoFixture;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
using Miniclip.Simulator.ReadModels.Projections.Services;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenProjectingGroupStandings;

public abstract class WhenProjectingGroupStandings : TestBase<GroupStandingsProjection>
{
    protected IGroupStandingsRepository Repository { get; private set; } = null!;
    protected IRecalculatePositionService RecalculatePositionService { get; private set; } = null!;
    protected MatchPlayed Event { get; set; } = null!;

    override protected void Given()
    {
        Repository = Fixture.Freeze<IGroupStandingsRepository>();
        RecalculatePositionService = Fixture.Freeze<IRecalculatePositionService>();
    }

    protected virtual void SetupRepositoryMock() { }

    protected override void When()
    {
        Sut!.Handle(Event, CancellationToken.None).Wait();
    }
}

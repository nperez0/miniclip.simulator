using AutoFixture;
using Miniclip.Core.Tests;
using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Projections.Services;
using Miniclip.Simulator.ReadModels.Repositories.Write;
using NSubstitute;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.Services;

public abstract class WhenRecalculatingPosition : TestBase<RecalculatePositionService>
{
    protected IMatchResultsRepository Repository { get; private set; } = null!;
    protected List<GroupStandingsModel> Standings { get; set; } = null!;
    protected Guid GroupId { get; set; }

    protected override void Given()
    {
        Repository = Fixture.Freeze<IMatchResultsRepository>();
    }

    protected override void When()
    {
        Sut!.RecalculatePositionsAsync(Standings, GroupId, CancellationToken.None).Wait();
    }
}

using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Projections.Services;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.Services;

public abstract class WhenRecalculatingPosition : AsyncTestBase<RecalculatePositionService>
{
    protected IMatchResultsRepository Repository { get; private set; } = null!;
    protected List<GroupStandingsModel> Standings { get; set; } = null!;
    protected Guid GroupId { get; set; }

    protected override Task GivenAsync()
    {
        Repository = Substitute.For<IMatchResultsRepository>();

        return GivenScenarioAsync();
    }

    protected override RecalculatePositionService CreateSystemUnderTest()
        => new(Repository);

    protected override async Task WhenAsync()
    {
        await Sut!.RecalculatePositionsAsync(Standings, GroupId, CancellationToken.None);
    }
}

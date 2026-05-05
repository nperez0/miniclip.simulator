using Miniclip.Simulator.IntegrationEvents.V1;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenProjectingMatchResults;

public abstract class WhenProjectingMatchResults : AsyncTestBase<MatchResultProjection>
{
    protected IMatchResultsRepository Repository { get; private set; } = null!;
    protected MatchPlayedIntegrationEvent Event { get; set; } = null!;

    protected override Task GivenAsync()
    {
        Repository = Substitute.For<IMatchResultsRepository>();

        return SetupScenarioAsync();
    }

    protected override MatchResultProjection CreateSystemUnderTest()
        => new(Repository);

    protected override async Task WhenAsync()
    {
        await Sut!.HandleAsync(Event, CancellationToken.None);
    }
}

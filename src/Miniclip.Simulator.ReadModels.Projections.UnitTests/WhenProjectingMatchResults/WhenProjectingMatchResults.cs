using AutoFixture;
using Miniclip.Core.Tests;
using Miniclip.Simulator.IntegrationEvents.V1;
using Miniclip.Simulator.ReadModels.Repositories.Write;
using NSubstitute;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenProjectingMatchResults;

public abstract class WhenProjectingMatchResults : TestBase<MatchResultProjection>
{
    protected IMatchResultsRepository Repository { get; private set; } = null!;
    protected MatchPlayedIntegrationEvent Event { get; set; } = null!;

    protected override void Given()
    {
        Repository = Fixture.Freeze<IMatchResultsRepository>();
    }

    protected override void When()
    {
        Sut!.HandleAsync(Event, CancellationToken.None).AsTask().Wait();
    }
}

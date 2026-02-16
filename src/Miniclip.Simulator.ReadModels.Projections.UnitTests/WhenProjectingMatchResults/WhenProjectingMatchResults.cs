using AutoFixture;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
using Miniclip.Simulator.ReadModels.Repositories.Write;
using NSubstitute;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenProjectingMatchResults;

public abstract class WhenProjectingMatchResults : TestBase<MatchResultProjection>
{
    protected IMatchResultsRepository Repository { get; private set; } = null!;
    protected MatchPlayed Event { get; set; } = null!;

    protected override void Given()
    {
        Repository = Fixture.Freeze<IMatchResultsRepository>();
    }

    protected override void When()
    {
        Sut!.Handle(Event, CancellationToken.None).Wait();
    }
}

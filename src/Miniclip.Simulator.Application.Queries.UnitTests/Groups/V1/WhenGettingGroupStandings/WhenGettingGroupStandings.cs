using AutoFixture;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Application.Queries.Groups.V1.Standings;
using Miniclip.Simulator.ReadModels.Repositories.Read;

namespace Miniclip.Simulator.Application.Queries.UnitTests.Groups.V1.WhenGettingGroupStandings;

public abstract class WhenGettingGroupStandings : TestBase<GroupStandingsQueryHandler>
{
    protected IGroupStandingsRepository StandingsRepository { get; private set; } = null!;
    protected IMatchResultsRepository MatchResultsRepository { get; private set; } = null!;
    protected GroupStandingsQuery Query { get; set; } = null!;
    protected GroupStandingsDto Result { get; set; } = null!;

    protected override void Given()
    {
        StandingsRepository = Fixture.Freeze<IGroupStandingsRepository>();
        MatchResultsRepository = Fixture.Freeze<IMatchResultsRepository>();
    }

    protected override void When()
    {
        Result = Sut!.Handle(Query, CancellationToken.None).Result;
    }
}

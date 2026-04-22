using Miniclip.Core;
using Miniclip.Simulator.Application.Queries.Groups.V1.Standings;
using Miniclip.Simulator.ReadModels.Repositories.Read;

namespace Miniclip.Simulator.Application.Queries.UnitTests.Groups.V1.WhenGettingGroupStandings;

public abstract class WhenGettingGroupStandings : AsyncTestBase<GroupStandingsQueryHandler>
{
    protected IGroupStandingsRepository StandingsRepository { get; private set; } = null!;
    protected IMatchResultsRepository MatchResultsRepository { get; private set; } = null!;
    protected GroupStandingsQuery Query { get; set; } = null!;
    protected Result<GroupStandingsDto> Result { get; set; } = null!;

    protected override Task GivenAsync()
    {
        StandingsRepository = Substitute.For<IGroupStandingsRepository>();
        MatchResultsRepository = Substitute.For<IMatchResultsRepository>();

        return GivenScenarioAsync();
    }

    protected override GroupStandingsQueryHandler CreateSystemUnderTest()
        => new(StandingsRepository, MatchResultsRepository);

    protected override async Task WhenAsync()
    {
        Result = await Sut!.Handle(Query, CancellationToken.None).ConfigureAwait(false);
    }
}

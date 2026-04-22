using Mediator;
using Microsoft.AspNetCore.Mvc;
using Miniclip.Core;
using Miniclip.Simulator.Application.Queries.Groups.V1.Standings;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenGettingStandings;

public class WithNoStandings : WhenGettingStandings
{
    private GroupStandingsDto emptyDto = null!;

    protected override async Task GivenAsync()
    {
        await base.GivenAsync();

        GroupId = Guid.NewGuid();

        emptyDto = new GroupStandingsDto();

        Mediator.Send(Arg.Any<GroupStandingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Result.Success(emptyDto)));
    }

    [Test]
    public void ShouldReturnOkResult()
    {
        ActionResult.ShouldBeOfType<OkObjectResult>();
    }

    [Test]
    public void ShouldReturnEmptyDto()
    {
        var okResult = ActionResult as OkObjectResult;
        var dto = okResult!.Value as GroupStandingsDto;
        dto.ShouldNotBeNull();
        dto!.GroupId.ShouldBe(Guid.Empty);
        dto.GroupName.ShouldBeEmpty();
    }

    [Test]
    public void ShouldSendQueryToMediator()
    {
        Mediator.Received(1).Send(
            Arg.Is<GroupStandingsQuery>(q => q.GroupId == GroupId),
            Arg.Any<CancellationToken>());
    }
}

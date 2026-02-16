using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Miniclip.Simulator.Application.Queries.Groups.V1.Standings;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenGettingStandings;

public class WithNoStandings : WhenGettingStandings
{
    private GroupStandingsDto emptyDto = null!;

    protected override void Given()
    {
        base.Given();

        GroupId = Guid.NewGuid();

        emptyDto = new GroupStandingsDto();

        Mediator.Send(Arg.Any<GroupStandingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(emptyDto));
    }

    [Test]
    public void ShouldReturnOkResult()
    {
        ActionResult.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public void ShouldReturnEmptyDto()
    {
        var okResult = ActionResult as OkObjectResult;
        var dto = okResult!.Value as GroupStandingsDto;
        dto.Should().NotBeNull();
        dto!.GroupId.Should().Be(Guid.Empty);
        dto.GroupName.Should().BeEmpty();
    }

    [Test]
    public void ShouldSendQueryToMediator()
    {
        Mediator.Received(1).Send(
            Arg.Is<GroupStandingsQuery>(q => q.GroupId == GroupId),
            Arg.Any<CancellationToken>());
    }
}

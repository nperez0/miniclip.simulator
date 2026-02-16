using AutoFixture;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Api.Controllers.V1;
using Miniclip.Simulator.Application.Queries.Groups.V1.Standings;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenGettingStandings;

public abstract class WhenGettingStandings : TestBase<GroupsController>
{
    protected IMediator Mediator { get; private set; } = null!;
    protected Guid GroupId { get; set; }
    protected IActionResult ActionResult { get; set; } = null!;

    protected override void Given()
    {
        Mediator = Fixture.Freeze<IMediator>();
    }

    protected override GroupsController CreateSystemUnderTest()
    {
        return new GroupsController(Mediator);
    }

    protected override void When()
    {
        ActionResult = Sut!.GetStandings(GroupId, CancellationToken.None).Result;
    }
}

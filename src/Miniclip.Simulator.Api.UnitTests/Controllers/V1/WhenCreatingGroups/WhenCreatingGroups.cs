using AutoFixture;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Api.Controllers.V1;
using Miniclip.Simulator.Application.Commands.Groups.V1.Generation;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenCreatingGroups;

public abstract class WhenCreatingGroups : TestBase<GroupsController>
{
    protected IMediator Mediator { get; private set; } = null!;
    protected GenerateGroupRequest Request { get; set; } = null!;
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
        ActionResult = Sut!.CreateGroup(Request, CancellationToken.None).Result;
    }
}

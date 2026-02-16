using AutoFixture;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Api.Controllers.V1;
using Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenSimulatingGroups;

public abstract class WhenSimulatingGroups : TestBase<GroupsController>
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
        ActionResult = Sut!.SimulateGroup(GroupId, CancellationToken.None).Result;
    }
}

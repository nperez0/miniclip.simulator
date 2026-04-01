using AutoFixture;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Api.Controllers.V1;
using Miniclip.Simulator.Application.Commands.Groups.V1.Generation;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenCreatingGroups;

public abstract class WhenCreatingGroups : AsyncTestBase<GroupsController>
{
    protected IMediator Mediator { get; private set; } = null!;
    protected GenerateGroupRequest Request { get; set; } = null!;
    protected IActionResult ActionResult { get; set; } = null!;

    protected override Task GivenAsync()
    {
        Mediator = Fixture.Freeze<IMediator>();

        return Task.CompletedTask;
    }

    protected override GroupsController CreateSystemUnderTest()
    {
        return new GroupsController(Mediator);
    }

    protected override async Task WhenAsync()
    {
        ActionResult = await Sut!.CreateGroup(Request, CancellationToken.None).ConfigureAwait(false);
    }
}

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
        Mediator = Substitute.For<IMediator>();

        return SetupScenarioAsync();
    }

    protected override GroupsController CreateSystemUnderTest()
        => new(Mediator);

    protected override async Task WhenAsync()
    {
        ActionResult = await Sut!.CreateGroup(Request, CancellationToken.None).ConfigureAwait(false);
    }
}

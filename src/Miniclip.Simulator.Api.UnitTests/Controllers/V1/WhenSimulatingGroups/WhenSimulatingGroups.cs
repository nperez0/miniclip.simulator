using Miniclip.Simulator.Api.Controllers.V1;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenSimulatingGroups;

public abstract class WhenSimulatingGroups : AsyncTestBase<GroupsController>
{
    protected IMediator Mediator { get; private set; } = null!;
    protected Guid GroupId { get; set; }
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
        ActionResult = await Sut!.SimulateGroup(GroupId, CancellationToken.None).ConfigureAwait(false);
    }
}

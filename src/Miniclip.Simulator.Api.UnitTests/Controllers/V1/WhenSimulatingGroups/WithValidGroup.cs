using Miniclip.Core;
using Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenSimulatingGroups;

public class WithValidGroup : WhenSimulatingGroups
{
    protected override Task GivenScenarioAsync()
    {
        GroupId = Guid.NewGuid();

        Mediator.Send(Arg.Any<SimulateGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Result.Success()));

        return Task.CompletedTask;
    }

    [Test]
    public void ShouldReturnNoContent()
    {
        ActionResult.ShouldBeOfType<NoContentResult>();
    }

    [Test]
    public void ShouldSendCommandToMediator()
    {
        Mediator.Received(1).Send(
            Arg.Is<SimulateGroupCommand>(cmd => cmd.GroupId == GroupId),
            Arg.Any<CancellationToken>());
    }
}

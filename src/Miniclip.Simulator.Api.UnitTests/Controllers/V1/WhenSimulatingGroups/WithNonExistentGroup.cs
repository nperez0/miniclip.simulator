using Miniclip.Core;
using Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenSimulatingGroups;

public class WithNonExistentGroup : WhenSimulatingGroups
{
    protected override async Task GivenAsync()
    {
        await base.GivenAsync();

        GroupId = Guid.NewGuid();

        var error = SimulateGroupErrors.GroupNotFound(GroupId);
        Mediator.Send(Arg.Any<SimulateGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Result.Failure(error)));
    }

    [Test]
    public void ShouldReturnNotFound()
    {
        ActionResult.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Test]
    public void ShouldReturnErrorMessage()
    {
        var notFoundResult = ActionResult as NotFoundObjectResult;
        notFoundResult!.Value.ShouldNotBeNull();

        var errorProperty = notFoundResult.Value!.GetType().GetProperty("error");
        var errorMessage = errorProperty!.GetValue(notFoundResult.Value) as string;
        errorMessage.ShouldBe($"Group {GroupId} not found");
    }

    [Test]
    public void ShouldSendCommandToMediator()
    {
        Mediator.Received(1).Send(
            Arg.Any<SimulateGroupCommand>(),
            Arg.Any<CancellationToken>());
    }
}

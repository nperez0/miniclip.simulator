using Miniclip.Core;
using Miniclip.Simulator.Application.Commands.Groups.V1.Generation;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenCreatingGroups;

public class WithInvalidRequest : WhenCreatingGroups
{
    protected override async Task GivenAsync()
    {
        await base.GivenAsync();

        Request = new GenerateGroupRequest("Group A", 10);

        var error = Error.Validation("GROUP_VALIDATION_FAILED", ["Group capacity must be between 2 and 6, but was 10."])
            with { Messages = ["Group capacity must be between 2 and 6, but was 10."] };

        Mediator
            .Send(Arg.Any<GenerateGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Result.Failure<Guid>(error)));
    }

    [Test]
    public void ShouldReturnBadRequest()
    {
        ActionResult.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Test]
    public void ShouldReturnErrorMessage()
    {
        var badRequestResult = ActionResult as BadRequestObjectResult;
        badRequestResult!.Value.ShouldNotBeNull();

        var errorsProperty = badRequestResult.Value!.GetType().GetProperty("errors");
        var errors = errorsProperty!.GetValue(badRequestResult.Value) as string[];
        errors![0].ShouldBe("Group capacity must be between 2 and 6, but was 10.");
    }

    [Test]
    public async Task ShouldSendCommandToMediator()
    {
        await Mediator.Received(1).Send(
            Arg.Any<GenerateGroupCommand>(),
            Arg.Any<CancellationToken>());
    }
}

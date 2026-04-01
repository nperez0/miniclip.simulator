using Shouldly;
using Microsoft.AspNetCore.Mvc;
using Miniclip.Core;
using Miniclip.Simulator.Application.Commands.Groups.V1.Generation;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenCreatingGroups;

public class WithInvalidRequest : WhenCreatingGroups
{
    protected override async Task GivenAsync()
    {
        await base.GivenAsync();

        Request = new GenerateGroupRequest("Group A", 10);

        var error = GroupCreationErrors.InvalidCapacity(10, 2, 6);

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
        
        var errorProperty = badRequestResult.Value!.GetType().GetProperty("error");
        var errorMessage = errorProperty!.GetValue(badRequestResult.Value) as string;
        errorMessage!.ShouldBe("Group capacity must be between 2 and 6, but was 10.");
    }

    [Test]
    public async Task ShouldSendCommandToMediator()
    {
        await Mediator.Received(1).Send(
            Arg.Any<GenerateGroupCommand>(),
            Arg.Any<CancellationToken>());
    }
}

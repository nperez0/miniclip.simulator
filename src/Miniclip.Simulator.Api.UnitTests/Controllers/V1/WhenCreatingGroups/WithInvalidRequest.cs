using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Miniclip.Core;
using Miniclip.Simulator.Application.Commands.Groups.V1.Generation;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenCreatingGroups;

public class WithInvalidRequest : WhenCreatingGroups
{
    protected override void Given()
    {
        base.Given();

        Request = new GenerateGroupRequest("Group A", 10);

        var exception = GroupCreationException.InvalidCapacity(10, 2, 6);

        Mediator.Send(Arg.Any<GenerateGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<Guid>(exception)));
    }

    [Test]
    public void ShouldReturnBadRequest()
    {
        ActionResult.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public void ShouldReturnErrorMessage()
    {
        var badRequestResult = ActionResult as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        var errorProperty = badRequestResult.Value!.GetType().GetProperty("error");
        var errorMessage = errorProperty!.GetValue(badRequestResult.Value) as string;
        errorMessage.Should().Contain("capacity");
    }

    [Test]
    public void ShouldSendCommandToMediator()
    {
        Mediator.Received(1).Send(
            Arg.Any<GenerateGroupCommand>(),
            Arg.Any<CancellationToken>());
    }
}

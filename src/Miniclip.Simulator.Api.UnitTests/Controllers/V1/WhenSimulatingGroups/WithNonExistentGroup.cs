using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Miniclip.Core;
using Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;
using NSubstitute;
using NUnit.Framework;
using System.Reflection;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenSimulatingGroups;

public class WithNonExistentGroup : WhenSimulatingGroups
{
    protected override void Given()
    {
        base.Given();

        GroupId = Guid.NewGuid();

        var exception = GroupNotFoundException.NotFound(GroupId);
        Mediator.Send(Arg.Any<SimulateGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(exception)));
    }

    [Test]
    public void ShouldReturnNotFound()
    {
        ActionResult.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public void ShouldReturnErrorMessage()
    {
        var notFoundResult = ActionResult as NotFoundObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
        
        var errorProperty = notFoundResult.Value!.GetType().GetProperty("error");
        var errorMessage = errorProperty!.GetValue(notFoundResult.Value) as string;
        errorMessage.Should().Contain("not found");
    }

    [Test]
    public void ShouldSendCommandToMediator()
    {
        Mediator.Received(1).Send(
            Arg.Any<SimulateGroupCommand>(),
            Arg.Any<CancellationToken>());
    }
}

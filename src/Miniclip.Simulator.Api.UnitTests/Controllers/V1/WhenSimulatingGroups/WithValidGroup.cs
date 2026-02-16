using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Miniclip.Core;
using Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenSimulatingGroups;

public class WithValidGroup : WhenSimulatingGroups
{
    protected override void Given()
    {
        base.Given();

        GroupId = Guid.NewGuid();

        Mediator.Send(Arg.Any<SimulateGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
    }

    [Test]
    public void ShouldReturnNoContent()
    {
        ActionResult.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public void ShouldSendCommandToMediator()
    {
        Mediator.Received(1).Send(
            Arg.Is<SimulateGroupCommand>(cmd => cmd.GroupId == GroupId),
            Arg.Any<CancellationToken>());
    }
}

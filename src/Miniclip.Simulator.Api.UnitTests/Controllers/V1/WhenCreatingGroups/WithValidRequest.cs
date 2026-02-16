using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Miniclip.Core;
using Miniclip.Simulator.Application.Commands.Groups.V1.Generation;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenCreatingGroups;

public class WithValidRequest : WhenCreatingGroups
{
    private Guid groupId;

    protected override void Given()
    {
        base.Given();

        Request = new GenerateGroupRequest("Group A", 4);
        groupId = Guid.NewGuid();

        Mediator.Send(Arg.Any<GenerateGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Guid>.Success(groupId)));
    }

    [Test]
    public void ShouldReturnOkResult()
    {
        ActionResult.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public void ShouldReturnGroupId()
    {
        var okResult = ActionResult as OkObjectResult;
        okResult!.Value.Should().Be(groupId);
    }

    [Test]
    public void ShouldSendCommandToMediator()
    {
        Mediator.Received(1).Send(
            Arg.Is<GenerateGroupCommand>(cmd => 
                cmd.Name == "Group A" && 
                cmd.Capacity == 4),
            Arg.Any<CancellationToken>());
    }
}

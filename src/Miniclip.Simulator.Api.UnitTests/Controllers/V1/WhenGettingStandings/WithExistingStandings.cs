using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Miniclip.Simulator.Application.Queries.Groups.V1.Standings;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Api.UnitTests.Controllers.V1.WhenGettingStandings;

public class WithExistingStandings : WhenGettingStandings
{
    private GroupStandingsDto standingsDto = null!;

    protected override void Given()
    {
        base.Given();

        GroupId = Guid.NewGuid();

        standingsDto = new GroupStandingsDto
        {
            GroupId = GroupId,
            GroupName = "Group A",
            Standings =
            [
                new TeamStandingDto
                {
                    Position = 1,
                    TeamId = Guid.NewGuid(),
                    TeamName = "Team 1",
                    TeamStrength = 85,
                    MatchesPlayed = 3,
                    Wins = 3,
                    Draws = 0,
                    Losses = 0,
                    GoalsFor = 8,
                    GoalsAgainst = 2,
                    GoalDifference = 6,
                    Points = 9,
                    QualifiesForKnockout = true
                },
                new TeamStandingDto
                {
                    Position = 2,
                    TeamId = Guid.NewGuid(),
                    TeamName = "Team 2",
                    TeamStrength = 80,
                    MatchesPlayed = 3,
                    Wins = 2,
                    Draws = 0,
                    Losses = 1,
                    GoalsFor = 6,
                    GoalsAgainst = 4,
                    GoalDifference = 2,
                    Points = 6,
                    QualifiesForKnockout = true
                }
            ],
            MatchResults =
            [
                new MatchResultDto
                {
                    MatchId = Guid.NewGuid(),
                    Round = 1,
                    HomeTeamName = "Team 1",
                    HomeScore = 3,
                    AwayTeamName = "Team 2",
                    AwayScore = 1,
                    PlayedAt = DateTime.UtcNow.AddDays(-1)
                }
            ]
        };

        Mediator.Send(Arg.Any<GroupStandingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(standingsDto));
    }

    [Test]
    public void ShouldReturnOkResult()
    {
        ActionResult.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public void ShouldReturnStandingsDto()
    {
        var okResult = ActionResult as OkObjectResult;
        okResult!.Value.Should().Be(standingsDto);
    }

    [Test]
    public void ShouldSendQueryToMediator()
    {
        Mediator.Received(1).Send(
            Arg.Is<GroupStandingsQuery>(q => q.GroupId == GroupId),
            Arg.Any<CancellationToken>());
    }
}

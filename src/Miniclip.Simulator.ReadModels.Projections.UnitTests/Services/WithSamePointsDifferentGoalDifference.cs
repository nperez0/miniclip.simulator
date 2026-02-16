using FluentAssertions;
using Miniclip.Simulator.ReadModels.Models;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.Services;

public class WithSamePointsDifferentGoalDifference : WhenRecalculatingPosition
{
    private GroupStandingsModel firstPlace = null!;
    private GroupStandingsModel secondPlace = null!;
    private GroupStandingsModel thirdPlace = null!;

    protected override void Given()
    {
        base.Given();

        GroupId = Guid.NewGuid();

        firstPlace = new GroupStandingsModel
        {
            Id = Guid.NewGuid(),
            GroupId = GroupId,
            TeamId = Guid.NewGuid(),
            TeamName = "Team A",
            Points = 6,
            GoalDifference = 5,
            GoalsFor = 8,
            GoalsAgainst = 3
        };

        secondPlace = new GroupStandingsModel
        {
            Id = Guid.NewGuid(),
            GroupId = GroupId,
            TeamId = Guid.NewGuid(),
            TeamName = "Team B",
            Points = 6,
            GoalDifference = 2,
            GoalsFor = 5,
            GoalsAgainst = 3
        };

        thirdPlace = new GroupStandingsModel
        {
            Id = Guid.NewGuid(),
            GroupId = GroupId,
            TeamId = Guid.NewGuid(),
            TeamName = "Team C",
            Points = 6,
            GoalDifference = 0,
            GoalsFor = 4,
            GoalsAgainst = 4
        };

        Standings = [thirdPlace, secondPlace, firstPlace];

        Repository.GetMatchResultsByGroupIdAsync(GroupId, default)
            .ReturnsForAnyArgs([]);
    }

    [Test]
    public void ShouldRankByGoalDifference()
    {
        firstPlace.Position.Should().Be(1);
        secondPlace.Position.Should().Be(2);
        thirdPlace.Position.Should().Be(3);
    }

    [Test]
    public void ShouldMarkTopTwoAsQualified()
    {
        firstPlace.QualifiesForKnockout.Should().BeTrue();
        secondPlace.QualifiesForKnockout.Should().BeTrue();
        thirdPlace.QualifiesForKnockout.Should().BeFalse();
    }
}

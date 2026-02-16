using FluentAssertions;
using Miniclip.Simulator.ReadModels.Models;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.Services;

public class WithDifferentPoints : WhenRecalculatingPosition
{
    private GroupStandingsModel firstPlace = null!;
    private GroupStandingsModel secondPlace = null!;
    private GroupStandingsModel thirdPlace = null!;
    private GroupStandingsModel fourthPlace = null!;

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
            Points = 9,
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
            Points = 3,
            GoalDifference = -2,
            GoalsFor = 3,
            GoalsAgainst = 5
        };

        fourthPlace = new GroupStandingsModel
        {
            Id = Guid.NewGuid(),
            GroupId = GroupId,
            TeamId = Guid.NewGuid(),
            TeamName = "Team D",
            Points = 0,
            GoalDifference = -5,
            GoalsFor = 2,
            GoalsAgainst = 7
        };

        Standings = [thirdPlace, firstPlace, fourthPlace, secondPlace];

        Repository.GetMatchResultsByGroupIdAsync(GroupId, default)
            .ReturnsForAnyArgs([]);
    }

    [Test]
    public void ShouldAssignCorrectPositions()
    {
        firstPlace.Position.Should().Be(1);
        secondPlace.Position.Should().Be(2);
        thirdPlace.Position.Should().Be(3);
        fourthPlace.Position.Should().Be(4);
    }

    [Test]
    public void ShouldMarkTopTwoAsQualified()
    {
        firstPlace.QualifiesForKnockout.Should().BeTrue();
        secondPlace.QualifiesForKnockout.Should().BeTrue();
        thirdPlace.QualifiesForKnockout.Should().BeFalse();
        fourthPlace.QualifiesForKnockout.Should().BeFalse();
    }

    [Test]
    public void ShouldUpdateLastUpdatedTimestamp()
    {
        firstPlace.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        secondPlace.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}

using FluentAssertions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingSchedule;

public class WithEmptyTeamsList : WhenGeneratingSchedule
{
    protected override void Given()
    {
        Capacity = 4;
        Teams = [];
    }

    [Test]
    public void ShouldGenerateEmptySchedule()
    {
        Schedule!.Should().BeEmpty();
    }
}

using NUnit.Framework;
using Shouldly;

namespace Miniclip.Core.Kafka.UnitTests.WhenExecutingConsumer;

[TestFixture]
public class AndHandlerAlwaysFails : WhenExecuting
{
    protected override async Task GivenAsync()
    {
        await base.GivenAsync();
        HandleAction = () => throw new InvalidOperationException("Permanent failure");
    }

    [Test]
    public void ShouldCallHandlerMaxAttemptsTimes()
        => Assert.That(Sut!.HandleCallCount, Is.EqualTo(3));

    [Test]
    public void ShouldRouteToDeadLetter()
        => Sut!.DeadLetterException.ShouldBeOfType<InvalidOperationException>();
}

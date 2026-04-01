using NSubstitute;
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
    public void ShouldCommitOffsetAfterDeadLetter()
        => MockConsumer.Received(1).Commit(TheResult);

    [Test]
    public void ShouldRouteToDeadLetter()
        => Sut!.DeadLetterException.ShouldBeOfType<InvalidOperationException>();
}

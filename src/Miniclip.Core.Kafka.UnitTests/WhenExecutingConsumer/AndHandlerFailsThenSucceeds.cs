using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Core.Kafka.UnitTests.WhenExecutingConsumer;

[TestFixture]
public class AndHandlerFailsThenSucceeds : WhenExecuting
{
    protected override void Given()
    {
        base.Given();

        var callCount = 0;
        HandleAction = () =>
        {
            if (++callCount == 1) throw new InvalidOperationException("Transient failure");
            return Task.CompletedTask;
        };
    }

    [Test]
    public void ShouldCallHandlerTwice()
        => Assert.That(Sut!.HandleCallCount, Is.EqualTo(2));

    [Test]
    public void ShouldCommitOffsetOnce()
        => MockConsumer.Received(1).Commit(TheResult);

    [Test]
    public void ShouldNotRouteToDeadLetter()
        => Assert.That(Sut!.DeadLetterException, Is.Null);
}

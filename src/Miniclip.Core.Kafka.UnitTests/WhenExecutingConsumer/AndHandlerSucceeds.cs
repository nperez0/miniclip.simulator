using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Core.Kafka.UnitTests.WhenExecutingConsumer;

[TestFixture]
public class AndHandlerSucceeds : WhenExecuting
{
    [Test]
    public void ShouldCallHandlerOnce()
        => Assert.That(Sut!.HandleCallCount, Is.EqualTo(1));

    [Test]
    public void ShouldCommitOffset()
        => MockConsumer.Received(1).Commit(TheResult);

    [Test]
    public void ShouldNotRouteToDeadLetter()
        => Assert.That(Sut!.DeadLetterException, Is.Null);
}

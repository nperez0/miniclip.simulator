using NUnit.Framework;

namespace Miniclip.Core.Kafka.UnitTests.WhenExecutingConsumer;

[TestFixture]
public class AndHandlerFailsThenSucceeds : WhenExecuting
{
    protected override async Task GivenAsync()
    {
        await base.GivenAsync();

        var callCount = 0;
        HandleAction = () => ++callCount == 1 
            ? throw new InvalidOperationException("Transient failure") 
            : Task.CompletedTask;
    }

    [Test]
    public void ShouldCallHandlerTwice()
        => Assert.That(Sut!.HandleCallCount, Is.EqualTo(2));

    [Test]
    public void ShouldNotRouteToDeadLetter()
        => Assert.That(Sut!.DeadLetterException, Is.Null);
}

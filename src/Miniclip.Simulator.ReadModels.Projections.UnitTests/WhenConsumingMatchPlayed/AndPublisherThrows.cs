using Mediator;
using Miniclip.Core.Domain;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenConsumingMatchPlayed;

[TestFixture]
public class AndPublisherThrows : WhenConsumingEvents
{
    protected override void Given()
    {
        base.Given();

        ConsumeResult = BuildConsumeResult(Guid.NewGuid().ToString(), "MatchPlayed");

        ProcessedEvents
            .ContainsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        Serializer
            .Deserialize(Arg.Any<string>(), Arg.Any<byte[]>())
            .Returns(Substitute.For<IDomainEvent>());

        Publisher
            .Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Publish failed"));

        RecordAnyExceptionsThrown();
    }

    [Test]
    public void ShouldRethrowException()
        => ThrownException.ShouldBeOfType<InvalidOperationException>();

    [Test]
    public async Task ShouldRollbackTransaction()
        => await Uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());

    [Test]
    public async Task ShouldNotCommit()
        => await Uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());

    [Test]
    public async Task ShouldNotSaveChanges()
        => await Uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
}

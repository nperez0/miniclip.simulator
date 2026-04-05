using Miniclip.Core.Domain;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenConsumingEvents;

[TestFixture]
public class AndMessageNotYetProcessed : WhenConsumingEvents
{
    private IDomainEvent domainEvent = null!;
    private string eventId = null!;

    protected override async Task GivenAsync()
    {
        await base.GivenAsync();

        eventId = Guid.NewGuid().ToString();
        domainEvent = Substitute.For<IDomainEvent>();
        Context = BuildKafkaMessageContext(eventId, "MatchPlayed");

        ProcessedEvents
            .ContainsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        Serializer
            .Deserialize(Arg.Any<string>(), Arg.Any<byte[]>())
            .Returns(domainEvent);
    }

    [Test]
    public async Task ShouldBeginTransaction()
        => await Uow.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());

    [Test]
    public async Task ShouldDispatchViaMediator()
        => await Publisher.Received(1).Publish(domainEvent, Arg.Any<CancellationToken>());

    [Test]
    public void ShouldRecordEventAsProcessed()
        => ProcessedEvents.Received(1).Add(eventId, Arg.Any<string>());

    [Test]
    public async Task ShouldSaveChanges()
        => await Uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

    [Test]
    public async Task ShouldCommit()
        => await Uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());

    [Test]
    public async Task ShouldNotRollback()
        => await Uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
}

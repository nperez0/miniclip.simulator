using Mediator;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenConsumingEvents;

[TestFixture]
public class AndMessageAlreadyProcessed : WhenConsumingEvents
{
    protected override async Task GivenAsync()
    {
        await base.GivenAsync();

        Context = BuildKafkaMessageContext(Guid.NewGuid().ToString(), "MatchPlayed");

        ProcessedEvents
            .ContainsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    [Test]
    public async Task ShouldNotBeginTransaction()
        => await Uow.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());

    [Test]
    public async Task ShouldNotPublishToMediator()
        => await Publisher.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());

    [Test]
    public void ShouldNotRecordAsProcessed()
        => ProcessedEvents.DidNotReceive().Add(Arg.Any<string>(), Arg.Any<string>());

    [Test]
    public async Task ShouldNotSaveChanges()
        => await Uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());

    [Test]
    public void ShouldNotDeserializePayload()
        => Serializer.DidNotReceive().Deserialize(Arg.Any<string>(), Arg.Any<byte[]>());
}

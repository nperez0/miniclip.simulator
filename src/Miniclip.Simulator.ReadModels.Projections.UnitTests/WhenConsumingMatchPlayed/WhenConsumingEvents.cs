using System.Text;
using AutoFixture;
using Confluent.Kafka;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.Kafka;
using Miniclip.Core.ReadModels;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
using Miniclip.Simulator.ReadModels.Repositories.Write;
using NSubstitute;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenConsumingMatchPlayed;

public abstract class WhenConsumingEvents : AsyncTestBase<TestableConsumer>
{
    protected IEventSerializer Serializer { get; private set; } = null!;
    protected IServiceScopeFactory ScopeFactory { get; private set; } = null!;
    protected IProcessedEventsRepository ProcessedEvents { get; private set; } = null!;
    protected IReadModelUnitOfWork Uow { get; private set; } = null!;
    protected IPublisher Publisher { get; private set; } = null!;
    protected ConsumeResult<string, byte[]>? ConsumeResult { get; set; }

    protected override void Given()
    {
        Serializer = Fixture.Freeze<IEventSerializer>();
        ProcessedEvents = Fixture.Freeze<IProcessedEventsRepository>();
        Uow = Fixture.Freeze<IReadModelUnitOfWork>();
        Publisher = Fixture.Freeze<IPublisher>();
        Fixture.Freeze<IConsumerRetryPolicy>();

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IProcessedEventsRepository)).Returns(ProcessedEvents);
        sp.GetService(typeof(IReadModelUnitOfWork)).Returns(Uow);
        sp.GetService(typeof(IPublisher)).Returns(Publisher);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(sp);

        ScopeFactory = Fixture.Freeze<IServiceScopeFactory>();
        ScopeFactory.CreateScope().Returns(scope);

        Fixture.Freeze<IConfiguration>();
        Fixture.Freeze<ILogger<ProjectionsConsumerService<MatchPlayed>>>();
    }

    protected override ValueTask WhenAsync()
        => new(Sut!.InvokeHandleAsync(ConsumeResult!, CancellationToken.None));

    protected static ConsumeResult<string, byte[]> BuildConsumeResult(string eventId, string eventType)
    {
        var headers = new Headers
        {
            { "event-id", Encoding.UTF8.GetBytes(eventId) },
            { "event-type", Encoding.UTF8.GetBytes(eventType) },
            { "occurred-on", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")) }
        };

        return new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = Guid.NewGuid().ToString(),
                Value = [],
                Headers = headers
            }
        };
    }
}


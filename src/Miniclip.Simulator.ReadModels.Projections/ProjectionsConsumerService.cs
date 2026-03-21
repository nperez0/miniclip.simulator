using Confluent.Kafka;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.Kafka;
using Miniclip.Core.ReadModels;
using Miniclip.Core.Domain;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.Projections;

public class ProjectionsConsumerService<TEvent>(
    IEventSerializer serializer,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ProjectionsConsumerService<TEvent>> logger)
    : KafkaConsumerService([TopicNaming.ForType<TEvent>()], configuration, logger)
    where TEvent : IDomainEvent
{
    protected override string ConsumerGroupId => "simulator-projections";

    protected override async Task HandleAsync(
        ConsumeResult<string, byte[]> result,
        CancellationToken cancellationToken)
    {
        var eventId = result.GetHeader("event-id");
        var eventType = result.GetHeader("event-type");
        var domainEvent = serializer.Deserialize(eventType, result.Message.Value);

        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var processedEvents = sp.GetRequiredService<IProcessedEventsRepository>();
        if (await processedEvents.ContainsAsync(eventId, ConsumerGroupId, cancellationToken))
            return;

        var uow = sp.GetRequiredService<IReadModelUnitOfWork>();
        var publisher = sp.GetRequiredService<IPublisher>();

        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            await publisher.Publish(domainEvent, cancellationToken);
            processedEvents.Add(eventId, ConsumerGroupId);
            await uow.SaveChangesAsync(cancellationToken);
            await uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

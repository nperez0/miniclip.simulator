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
    ILogger<ProjectionsConsumerService<TEvent>> logger,
    IConsumerRetryPolicy retryPolicy)
    : KafkaConsumerService([TopicNaming.ForType<TEvent>()], configuration, logger, retryPolicy)
    where TEvent : IDomainEvent
{
    protected override string ConsumerGroupId
        => $"simulator-projections-{TopicNaming.ForType<TEvent>().Replace("simulator.", string.Empty)}";

    protected override async Task HandleAsync(
        ConsumeResult<string, byte[]> result,
        CancellationToken cancellationToken)
    {
        var eventId = result.GetHeader("event-id");

        using var scope = scopeFactory.CreateScope();
        var processedEventsRepository = scope.ServiceProvider.GetRequiredService<IProcessedEventsRepository>();

        if (await processedEventsRepository.ContainsAsync(eventId, ConsumerGroupId, cancellationToken))
            return;

        var eventType = result.GetHeader("event-type");
        var domainEvent = serializer.Deserialize(eventType, result.Message.Value);

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IReadModelUnitOfWork>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await publisher.Publish(domainEvent, cancellationToken);
            processedEventsRepository.Add(eventId, ConsumerGroupId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

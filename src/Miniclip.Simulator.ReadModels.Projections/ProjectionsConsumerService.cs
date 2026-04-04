using Confluent.Kafka;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniclip.Core.Domain;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.Kafka;
using Miniclip.Core.Kafka.OpenTelemetry;
using Miniclip.Core.ReadModels;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.Projections;

public partial class ProjectionsConsumerService<TAggregate>(
    IKafkaConsumerConfig config,
    IServiceScopeFactory scopeFactory,
    IKafkaConsumerFactory consumerFactory,
    IConsumerRetryPolicy retryPolicy,
    IEventSerializer serializer,
    ITelemetryRecorderFactory telemetryRecorderFactory,
    ILogger<ProjectionsConsumerService<TAggregate>> logger
    ) : KafkaConsumerService(config, consumerFactory, retryPolicy, telemetryRecorderFactory, logger)
    where TAggregate : AggregateRoot
{
    protected override async Task HandleAsync(
        ConsumeResult<string, byte[]> result,
        CancellationToken cancellationToken)
    {
        var eventId = result.GetHeader("event-id");
        var eventType = result.GetHeader("event-type");

        using var scope = scopeFactory.CreateScope();
        var processedEventsRepository = scope.ServiceProvider.GetRequiredService<IProcessedEventsRepository>();

        if (await processedEventsRepository.ContainsAsync(eventId, Config.ConsumerGroupId, cancellationToken))
        {
            LogEventSkipped(logger, eventId, eventType, Config.ConsumerGroupId);
            return;
        }

        var domainEvent = serializer.Deserialize(eventType, result.Message.Value);

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IReadModelUnitOfWork>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await publisher.Publish(domainEvent, cancellationToken);
            processedEventsRepository.Add(eventId, Config.ConsumerGroupId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            LogEventProjected(logger, eventId, eventType, Config.ConsumerGroupId);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    [LoggerMessage(LogLevel.Information,
        "Event {EventId} ({EventType}) skipped — already processed by {ConsumerGroup}")]
    static partial void LogEventSkipped(
        ILogger<ProjectionsConsumerService<TAggregate>> logger,
        string EventId, string EventType, string ConsumerGroup);

    [LoggerMessage(LogLevel.Information,
        "Event {EventId} ({EventType}) projected by {ConsumerGroup}")]
    static partial void LogEventProjected(
        ILogger<ProjectionsConsumerService<TAggregate>> logger,
        string EventId, string EventType, string ConsumerGroup);
}

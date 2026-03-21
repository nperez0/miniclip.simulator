using Confluent.Kafka;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.Kafka;

namespace Miniclip.Simulator.ReadModels.Projections;

// Temporary bridge — replaced by dedicated Kafka consumer handlers in Phase 4
public class MatchPlayedKafkaRelayService(
    IEventSerializer serializer,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<MatchPlayedKafkaRelayService> logger)
    : KafkaConsumerService(["simulator.match-played"], configuration, logger)
{
    protected override string ConsumerGroupId => "simulator-projections";

    protected override async Task HandleAsync(
        ConsumeResult<string, byte[]> result,
        CancellationToken cancellationToken)
    {
        var eventType = result.GetHeader("event-type");
        var domainEvent = serializer.Deserialize(eventType, result.Message.Value);

        using var scope = scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.Publish(domainEvent, cancellationToken);
    }
}

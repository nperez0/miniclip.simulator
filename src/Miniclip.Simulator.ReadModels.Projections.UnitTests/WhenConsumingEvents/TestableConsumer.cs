using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.Kafka;
using Miniclip.Core.Kafka.OpenTelemetry;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenConsumingEvents;

public sealed class TestableConsumer(
    IEventSerializer serializer,
    IServiceScopeFactory scopeFactory,
    IKafkaConsumerConfig config,
    IKafkaConsumerFactory consumerFactory,
    IConsumerRetryPolicy retryPolicy,
    ITelemetryRecorderFactory telemetryRecorderFactory,
    ILogger<ProjectionsConsumerService<Group>> logger)
    : ProjectionsConsumerService<Group>(config, scopeFactory, consumerFactory, retryPolicy, serializer, telemetryRecorderFactory, logger)
{
    public Task InvokeHandleAsync(ConsumeResult<string, byte[]> result, CancellationToken ct)
        => HandleAsync(result, ct);
}

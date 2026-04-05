using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.Kafka;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenConsumingEvents;

public sealed class TestableConsumer(
    IEventSerializer serializer,
    IServiceScopeFactory scopeFactory,
    IKafkaConsumerConfig config,
    IKafkaConsumerFactory consumerFactory,
    IConsumerRetryPolicy retryPolicy,
    ILogger<ProjectionsConsumerService<Group>> logger)
    : ProjectionsConsumerService<Group>(
        config, 
        scopeFactory,
        consumerFactory,
        retryPolicy, 
        serializer, 
        logger)
{
    public Task InvokeHandleAsync(KafkaMessageContext context, CancellationToken ct)
        => HandleAsync(context, ct);
}

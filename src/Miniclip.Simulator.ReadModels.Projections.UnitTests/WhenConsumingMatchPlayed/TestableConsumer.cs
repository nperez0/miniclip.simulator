using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniclip.Core.EventSourcing;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenConsumingMatchPlayed;

public sealed class TestableConsumer(
    IEventSerializer serializer,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ProjectionsConsumerService<MatchPlayed>> logger)
    : ProjectionsConsumerService<MatchPlayed>(serializer, scopeFactory, configuration, logger)
{
    public Task InvokeHandleAsync(ConsumeResult<string, byte[]> result, CancellationToken ct)
        => HandleAsync(result, ct);
}

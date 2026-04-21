using Miniclip.Core.Application.Configuration;
using Miniclip.Core.Domain;
using Miniclip.Core.Messaging.Inbound;
using Miniclip.Core.Messaging.Kafka;
using Miniclip.Core.Messaging.Kafka.Configuration;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Write;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.WebJob.Infrastructure.Configuration;

public static class KafkaConfiguration
{
    public static IServiceCollection AddKafkaDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var kafkaConnectionString = configuration.GetConnectionString("kafka");

        services.AddIntegrationEventSerializer();

        services.AddScoped<IProcessedEventsRepository, ProcessedEventsRepository>();

        // Register shared pipeline + Kafka infrastructure (producer, DLQ handler) once
        services.AddFullKafkaInfrastructure(
            kafkaConnectionString!,
            options =>
        {
            options.RetryPolicy = new ExponentialBackoffRetryPolicy(
                maxAttempts: 3,
                initialDelayMs: 100,
                backoffMultiplier: 2.0,
                maxDelayMs: 5000);
        });

        // Register one KafkaConsumerHost per topic/consumer-group.
        // Each call is independent — configs are captured in factory closures, never in DI.
        services.AddKafkaConsumer(BuildConsumerConfig<Group>(kafkaConnectionString!));

        return services;
    }

    private static KafkaConsumerConfig BuildConsumerConfig<TAggregate>(string connectionString)
        where TAggregate : AggregateRoot
        => new()
        {
            BootstrapServers = connectionString,
            ConsumerGroup = new ConsumerGroup($"simulator-projections-{ConsumerGroupIdNaming.ForAggregate<TAggregate>()}"),
            Topics = [TopicNaming.ForAggregate<TAggregate>()],
            ConsumerCount = 1
        };

}

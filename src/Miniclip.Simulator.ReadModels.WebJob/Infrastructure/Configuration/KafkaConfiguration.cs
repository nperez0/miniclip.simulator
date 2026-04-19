using Miniclip.Core.Application.Configuration;
using Miniclip.Core.Domain;
using Miniclip.Core.Messaging.Inbound;
using Miniclip.Core.Messaging.Kafka;
using Miniclip.Core.Messaging.Kafka.Configuration;
using Miniclip.Core.Messaging.Pipeline.Configuration;
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

        // Message type registry + serializer (replaces the old EventSerializerAdapter)
        services.AddMessageTypeRegistry();
        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

        services.AddScoped<IProcessedEventsRepository, ProcessedEventsRepository>();

        services.AddMessageHandlers();

        // Register shared pipeline + Kafka infrastructure (producer, DLQ handler) once
        services.AddKafkaMessagingInfrastructure(
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

    private static IKafkaConsumerConfig BuildConsumerConfig<TAggregate>(string connectionString)
        where TAggregate : AggregateRoot
        => new KafkaConsumerConfig
        {
            BootstrapServers = connectionString,
            ConsumerGroup = new ConsumerGroup($"simulator-projections-{ConsumerGroupIdNaming.ForAggregate<TAggregate>()}"),
            Topics = [TopicNaming.ForAggregate<TAggregate>()],
            ConsumerCount = 1
        };

}

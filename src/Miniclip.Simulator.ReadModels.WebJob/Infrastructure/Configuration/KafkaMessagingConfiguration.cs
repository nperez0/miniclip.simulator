using Miniclip.Core.Application.Serializers;
using Miniclip.Core.Domain;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.Messaging;
using Miniclip.Core.Messaging.Kafka;
using Miniclip.Core.Messaging.Kafka.Configuration;
using Miniclip.Core.Messaging.Pipeline.Configuration;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Write;
using Miniclip.Simulator.ReadModels.Projections;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.WebJob.Infrastructure.Configuration;

public static class KafkaMessagingConfiguration
{
    public static IReadOnlyDictionary<string, string> ConsumerGroupIds = new Dictionary<string, string> 
    {
        { typeof(Group).FullName!, $"simulator-projections-{ConsumerGroupIdNaming.ForAggregate<Group>()}" }
    };

    public static IServiceCollection AddProjectionsKafkaMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var kafkaConnectionString = configuration.GetConnectionString("kafka");

        // Register the base event serializer (already exists)
        services.AddSingleton<IEventSerializer, DomainEventJsonSerializer>();

        // Register message serializer as adapter to IEventSerializer
        services.AddSingleton<IMessageSerializer, EventSerializerAdapter>();

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
            ConsumerGroupId = ConsumerGroupIds[typeof(TAggregate).FullName!],
            Topics = [TopicNaming.ForAggregate<TAggregate>()],
            ConsumerCount = 1
        };

    }

internal sealed class EventSerializerAdapter(IEventSerializer eventSerializer) : IMessageSerializer
{
    public object Deserialize(string messageType, ReadOnlyMemory<byte> payload)
    {
        return eventSerializer.Deserialize(messageType, payload.ToArray());
    }
}

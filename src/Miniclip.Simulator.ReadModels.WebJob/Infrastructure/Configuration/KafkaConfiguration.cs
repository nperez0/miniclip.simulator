using Miniclip.Core.Messaging.Inbound;
using Miniclip.Core.Messaging.Kafka.Configuration;
using Miniclip.Simulator.IntegrationEvents;
using Miniclip.Simulator.IntegrationEvents.V1;

namespace Miniclip.Simulator.ReadModels.WebJob.Infrastructure.Configuration;

public static class KafkaConfiguration
{
    private const string GroupConsumerId = "simulator-readmodels-webjob-group";

    public static IServiceCollection AddKafkaDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var kafkaConnectionString = configuration.GetConnectionString("kafka")!;

        services.AddKafka(kafkaConnectionString, kafka =>
        {
            kafka.ConfigureInbound(inbound =>
            {
                inbound
                    .UseRetryPolicy(new ExponentialBackoffRetryPolicy(
                        maxAttempts: 3,
                        initialDelayMs: 100,
                        backoffMultiplier: 2.0,
                        maxDelayMs: 5000))
                    .AddConsumer(consumer => consumer
                        .WithConsumerGroup(GroupConsumerId)
                        .WithTopics(SimulatorTopics.Group)
                        .Handles<MatchPlayedIntegrationEvent>()
                        .WithConsumerCount(1));
            });
        });

        return services;
    }
}
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Miniclip.Core.Application;
using Miniclip.Core.Domain;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.Kafka;
using Miniclip.Core.Kafka.OpenTelemetry;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Write;
using Miniclip.Simulator.ReadModels.Projections;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class KafkaConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddKafkaDependencies(
        IConfiguration configuration)
        {
            services.AddEventBus(configuration);

            services.AddSingleton<IKafkaConsumerFactory, KafkaConsumerFactory>();
            services.AddSingleton<IConsumerRetryPolicy, ExponentialBackoffRetryPolicy>();
            services.AddSingleton<ITelemetryRecorderFactory, KafkaTelemetryRecorderFactory>();
            services.AddScoped<IProcessedEventsRepository, ProcessedEventsRepository>();

            services.AddProjectionsConsumers();

            return services;
        }

        private IServiceCollection AddEventBus(
            IConfiguration configuration)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration.GetConnectionString("kafka")!
            };

            services.AddSingleton<IProducer<string, byte[]>>(_ =>
                new ProducerBuilder<string, byte[]>(config).Build());

            services.AddSingleton<IEventBus, KafkaEventBus>();

            return services;
        }

        private IServiceCollection AddProjectionsConsumers()
            => services.AddHostedService(BuildProjectionsConsumerFor<Group>);
    }

    private static ProjectionsConsumerService<TAggregate> BuildProjectionsConsumerFor<TAggregate>(this IServiceProvider service) where TAggregate : AggregateRoot
    {
        var config = new KafkaConsumerConfig
        {
            BootstrapServers = service.GetRequiredService<IConfiguration>().GetConnectionString("kafka")!,
            ConsumerGroupId = $"simulator-projections-{ConsumerGroupIdNaming.ForAggregate<Group>()}",
            Topics = [TopicNaming.ForAggregate<Group>()]
        };

        var serviceFactory = service.GetRequiredService<IServiceScopeFactory>();
        var consumerFactory = service.GetRequiredService<IKafkaConsumerFactory>();
        var retryPolicy = service.GetRequiredService<IConsumerRetryPolicy>();
        var serializer = service.GetRequiredService<IEventSerializer>();
        var logger = service.GetRequiredService<ILogger<ProjectionsConsumerService<TAggregate>>>();
        var telemetryRecorderFactory = service.GetRequiredService<ITelemetryRecorderFactory>();

        return new ProjectionsConsumerService<TAggregate>(config, serviceFactory, consumerFactory, retryPolicy, serializer, telemetryRecorderFactory, logger);
    }
}

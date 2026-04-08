using Confluent.Kafka;
using Miniclip.Core.Application.Serializers;
using Miniclip.Core.Domain;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.Kafka;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Write;
using Miniclip.Simulator.ReadModels.Projections;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.WebJob.Infrastructure.Configuration;

public static class KafkaConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddProjectionsKafkaDependencies(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("kafka");

            services.AddSingleton<IEventSerializer, DomainEventJsonSerializer>();
            services.AddSingleton<IConsumerRetryPolicy, ExponentialBackoffRetryPolicy>();
            services.AddScoped<IProcessedEventsRepository, ProcessedEventsRepository>();

            services.AddProjectionsConsumer<Group>(connectionString);

            return services;
        }

        private IServiceCollection AddProjectionsConsumer<TAggregate>(string? connectionString)
            where TAggregate : AggregateRoot
        {
            var key = TopicNaming.ForAggregate<TAggregate>();
            var config = new KafkaConsumerConfig
            {
                BootstrapServers = connectionString!,
                ConsumerGroupId = BuildProjectionsConsumerGroupIdFor<TAggregate>(),
                Topics = [TopicNaming.ForAggregate<TAggregate>()]
            };

            services.AddKeyedSingleton<InstrumentedConsumerBuilder<string, byte[]>, InstrumentedConsumerBuilder<string, byte[]>>(
                key,
                (_, _) => new InstrumentedConsumerBuilder<string, byte[]>(config.ConsumerConfig));

            return services
                .AddHostedService<ProjectionsConsumerService<TAggregate>>(sp => BuildProjectionsConsumerFor<TAggregate>(sp, config, key));
        }
    }

    private static ProjectionsConsumerService<TAggregate> BuildProjectionsConsumerFor<TAggregate>(
        IServiceProvider services,
        KafkaConsumerConfig config,
        string key)
        where TAggregate : AggregateRoot
    {
        var consumerFactory = new KafkaConsumerFactory(
            services.GetKeyedService<InstrumentedConsumerBuilder<string, byte[]>>(key)!,
            config,
            services.GetRequiredService<ILogger<KafkaConsumer>>());

        return new ProjectionsConsumerService<TAggregate>(
            config,
            services.GetRequiredService<IServiceScopeFactory>(),
            consumerFactory,
            services.GetRequiredService<IConsumerRetryPolicy>(),
            services.GetRequiredService<IEventSerializer>(),
            services.GetRequiredService<ILogger<ProjectionsConsumerService<TAggregate>>>());
    }

    private static string BuildProjectionsConsumerGroupIdFor<TAggregate>() where TAggregate : AggregateRoot
        => $"simulator-projections-{ConsumerGroupIdNaming.ForAggregate<TAggregate>()}";
}

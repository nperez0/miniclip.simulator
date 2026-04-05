using Confluent.Kafka;
using Miniclip.Core.Application;
using Miniclip.Core.Domain;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.Kafka;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Write;
using Miniclip.Simulator.ReadModels.Projections;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class KafkaConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddKafkaDependencies(IConfiguration configuration)
        {
            services.AddEventBus(configuration);

            services.AddSingleton<IConsumerRetryPolicy, ExponentialBackoffRetryPolicy>();
            services.AddScoped<IProcessedEventsRepository, ProcessedEventsRepository>();

            services.AddProjectionsConsumers(configuration);

            return services;
        }

        private IServiceCollection AddEventBus(IConfiguration configuration)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration.GetConnectionString("kafka")!
            };

            services.AddSingleton(new InstrumentedProducerBuilder<string, byte[]>(config));

            services.AddSingleton<IProducer<string, byte[]>>(sp =>
                sp.GetRequiredService<InstrumentedProducerBuilder<string, byte[]>>().Build());

            services.AddSingleton<IEventBus, KafkaEventBus>();

            return services;
        }

        private IServiceCollection AddProjectionsConsumers(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("kafka");

            return services.AddProjectionsConsumer<Group>(connectionString);
        }

        private IServiceCollection AddProjectionsConsumer<TAggregate>(string? connectionString) where TAggregate : AggregateRoot
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
        IServiceProvider service, 
        KafkaConsumerConfig config,
        string key) 
        where TAggregate : AggregateRoot
    {
        var consumerFactory = new KafkaConsumerFactory(
            service.GetKeyedService<InstrumentedConsumerBuilder<string, byte[]>>(key)!,
            config,
            service.GetRequiredService<ILogger<KafkaConsumer>>());

        var serviceFactory = service.GetRequiredService<IServiceScopeFactory>();
        var retryPolicy = service.GetRequiredService<IConsumerRetryPolicy>();
        var serializer = service.GetRequiredService<IEventSerializer>();
        var logger = service.GetRequiredService<ILogger<ProjectionsConsumerService<TAggregate>>>();

        return new ProjectionsConsumerService<TAggregate>(config, serviceFactory, consumerFactory, retryPolicy, serializer, logger);
    }

    private static string BuildProjectionsConsumerGroupIdFor<TAggregate>() where TAggregate : AggregateRoot
        => $"simulator-projections-{ConsumerGroupIdNaming.ForAggregate<TAggregate>()}";
}

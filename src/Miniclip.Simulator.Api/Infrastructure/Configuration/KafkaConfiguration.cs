using Confluent.Kafka;
using Miniclip.Core.Application;
using Miniclip.Core.Domain;
using Miniclip.Core.Kafka;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
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

            services.AddSingleton<IConsumerRetryPolicy, ExponentialBackoffRetryPolicy>();
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
    }

    extension(IServiceCollection services)
    {
        private IServiceCollection AddProjectionsConsumers()
        {
            return services
                .AddHostedService<ProjectionsConsumerService<MatchPlayed>>();
        }
    }
}

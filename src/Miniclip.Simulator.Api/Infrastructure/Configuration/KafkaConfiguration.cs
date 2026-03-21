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
    public static IServiceCollection AddKafkaDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEventBus(configuration);

        services.AddSingleton<IConsumerRetryPolicy, ExponentialBackoffRetryPolicy>();
        services.AddScoped<IProcessedEventsRepository, ProcessedEventsRepository>();

        services.AddProjectionsConsumers();

        return services;
    }

    private static IServiceCollection AddEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var config = new ProducerConfig { 
            BootstrapServers = configuration.GetConnectionString("kafka")!
        };

        services.AddSingleton<IProducer<string, byte[]>>(_ =>
            new ProducerBuilder<string, byte[]>(config).Build());

        services.AddSingleton<IEventBus, KafkaEventBus>();

        return services;
    }

    private static IServiceCollection AddProjectionsConsumers(this IServiceCollection services)
    {
        return services
            .AddHostedService<ProjectionsConsumerService<MatchPlayed>>();
    }
}

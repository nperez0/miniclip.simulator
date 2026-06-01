using KurrentDB.Client;
using Miniclip.Core.Application.Configuration;
using Miniclip.Core.EventSourcing.EventStoreDB.Configuration;
using Miniclip.Core.Messaging.Kafka.Configuration;
using Miniclip.Simulator.IntegrationEvents;
using Miniclip.Simulator.IntegrationEvents.V1;

namespace Miniclip.Simulator.EventRelay.WebJob.Infrastructure.Configuration;

public static class EventRelayConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddEventRelayDependencies(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("EventStore")!;
            var settings = KurrentDBClientSettings.Create(connectionString);

            services.AddSingleton(_ => new KurrentDBClient(settings));
            services.AddSingleton(_ => new KurrentDBPersistentSubscriptionsClient(settings));

            services.AddEventStoreInfrastructure();
            services.AddIntegrationEventMappers();

            services.AddHostedService<KurrentDbForwarderService>();

            return services;
        }

        public IServiceCollection AddKafkaDependencies(IConfiguration configuration)
        {
            var bootstrapServers = configuration.GetConnectionString("kafka")!;

            services.AddKafka(bootstrapServers, kafka =>
            {
                kafka.ConfigureOutbound(outbound =>
                    outbound.MapTopic<MatchPlayedIntegrationEvent>(SimulatorTopics.Group));
            });

            return services;
        }
    }
}

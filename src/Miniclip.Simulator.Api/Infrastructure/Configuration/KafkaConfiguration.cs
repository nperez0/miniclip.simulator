using Miniclip.Core.Application.Configuration;
using Miniclip.Core.Messaging.Kafka.Configuration;
using Miniclip.Simulator.IntegrationEvents;
using Miniclip.Simulator.IntegrationEvents.V1;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class KafkaConfiguration
{
    extension(IServiceCollection services)
    {
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
using Miniclip.Core.Application.Configuration;
using Miniclip.Core.Messaging.Kafka.Configuration;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class KafkaConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddKafkaDependencies(IConfiguration configuration)
        {
            var bootstrapServers = configuration.GetConnectionString("kafka")!;

            services.AddIntegrationEventMappers();
            services.AddIntegrationEventSerializer();

            services.AddOutboundKafkaInfrastructure(bootstrapServers);

            return services;
        }
    }
}

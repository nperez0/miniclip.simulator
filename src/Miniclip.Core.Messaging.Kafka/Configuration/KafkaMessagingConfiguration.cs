using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Messaging.Kafka.Configuration.Builders;

namespace Miniclip.Core.Messaging.Kafka.Configuration;

public static class KafkaMessagingConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddKafka(string bootstrapServers, Action<KafkaBuilder> configure)
        {
            var builder = new KafkaBuilder(services, bootstrapServers);

            configure(builder);

            builder.Build();

            return services;
        }
    }
}


using Confluent.Kafka;
using Miniclip.Core.Messaging;
using Miniclip.Core.Messaging.Kafka;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class KafkaConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddKafkaDependencies(IConfiguration configuration)
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
    }
}


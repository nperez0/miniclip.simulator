using Confluent.Kafka;
using Miniclip.Core.Application.Configuration;
using Miniclip.Core.Messaging;
using Miniclip.Core.Messaging.Inbound;
using Miniclip.Core.Messaging.Kafka;
using Miniclip.Core.Messaging.Outbound;
using Miniclip.Core.Messaging.Pipeline.Configuration;

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

            services.AddMessageTypeRegistry();
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

            services.AddOutboundPipeline();
            services.AddSingleton<IEventDispatcher, KafkaEventDispatcher>();

            return services;
        }
    }
}

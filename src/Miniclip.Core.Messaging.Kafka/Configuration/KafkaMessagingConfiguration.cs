using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniclip.Core.Messaging.Inbound;
using Miniclip.Core.Messaging.Outbound;
using Miniclip.Core.Messaging.Pipeline.Configuration;

namespace Miniclip.Core.Messaging.Kafka.Configuration;

public static class KafkaMessagingConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddKafkaMessagingInfrastructure(
            string bootstrapServers, 
            Action<PipelineOptions>? configurePipeline = null)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            services.AddMessagingPipeline(configurePipeline);
            services.AddOutboundPipeline();

            services.AddSingleton(new InstrumentedProducerBuilder<string, byte[]>(config));

            services.AddSingleton<IProducer<string, byte[]>>(sp =>
                sp.GetRequiredService<InstrumentedProducerBuilder<string, byte[]>>().Build());

            services.AddSingleton<IDeadLetterHandler, KafkaDeadLetterHandler>();

            services.AddScoped<IEventDispatcher, KafkaEventDispatcher>();

            return services;
        }

        public IServiceCollection AddKafkaConsumer(IKafkaConsumerConfig config)
        {
            var consumerBuilder = new InstrumentedConsumerBuilder<string, byte[]>(config.ConsumerConfig);

            services.AddKeyedSingleton(config.ConsumerGroup.Id, consumerBuilder);
            services.AddSingleton(config.ConsumerGroup);

            services.AddHostedService(sp => new KafkaConsumerHost(
                config,
                consumerBuilder,
                sp.GetRequiredService<IInboundPipeline>(),
                sp.GetRequiredService<IDeadLetterHandler>(),
                sp.GetRequiredService<ILogger<KafkaConsumerHost>>()));

            return services;
        }
    }
}

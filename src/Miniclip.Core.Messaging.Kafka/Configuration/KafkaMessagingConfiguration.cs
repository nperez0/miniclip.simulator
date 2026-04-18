using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            services.AddMessagingPipeline(configurePipeline);

            services.AddSingleton<IProducer<string, byte[]>>(_ =>
                new ProducerBuilder<string, byte[]>(
                        new ProducerConfig { BootstrapServers = bootstrapServers })
                    .Build());

            services.AddSingleton<IDeadLetterHandler, KafkaDeadLetterHandler>();

            return services;
        }

        public IServiceCollection AddKafkaConsumer(IKafkaConsumerConfig config)
        {
            var consumerBuilder = new InstrumentedConsumerBuilder<string, byte[]>(config.ConsumerConfig);

            services.AddKeyedSingleton(config.ConsumerGroupId, consumerBuilder);

            services.AddHostedService(sp => new KafkaConsumerHost(
                config,
                consumerBuilder,
                sp.GetRequiredService<IMessagePipeline>(),
                sp.GetRequiredService<IDeadLetterHandler>(),
                sp.GetRequiredService<ILogger<KafkaConsumerHost>>()));

            return services;
        }
    }
}

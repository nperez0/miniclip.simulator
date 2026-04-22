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
        public IServiceCollection AddFullKafkaInfrastructure(
            string bootstrapServers,
            Action<PipelineOptions>? configurePipeline = null)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            services.AddInboundPipeline(configurePipeline);
            services.AddOutboundPipeline();

            services.AddMessageHandlers();

            services.AddSingleton(new InstrumentedProducerBuilder<string, byte[]>(config));

            services.AddSingleton<IProducer<string, byte[]>>(sp =>
                sp.GetRequiredService<InstrumentedProducerBuilder<string, byte[]>>().Build());

            services.AddSingleton<IDeadLetterHandler, KafkaDeadLetterHandler>();

            services.AddScoped<IEventDispatcher, KafkaEventDispatcher>();

            return services;
        }

        public IServiceCollection AddOutboundKafkaInfrastructure(string bootstrapServers)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            services.AddOutboundPipeline();

            services.AddSingleton(new InstrumentedProducerBuilder<string, byte[]>(config));

            services.AddSingleton<IProducer<string, byte[]>>(sp =>
                sp.GetRequiredService<InstrumentedProducerBuilder<string, byte[]>>().Build());
            
            services.AddSingleton<IEventDispatcher, KafkaEventDispatcher>();

            return services;
        }

        public IServiceCollection AddKafkaConsumer(IKafkaConsumerConfig config)
        {
            // One builder per consumer group — keyed so OTel can resolve it via
            // AddKafkaConsumerInstrumentation(group.Id) in the OTel configuration.
            var consumerBuilder = new InstrumentedConsumerBuilder<string, byte[]>(config.ConsumerConfig);
            services.AddKeyedSingleton(config.ConsumerGroup.Id, consumerBuilder);
            services.AddSingleton(config.ConsumerGroup);

            // Register ConsumerCount hosted service instances, each sharing the same
            // builder but calling .Build() independently — safe because the builder
            // is stateless config; each .Build() produces a fresh IConsumer.
            for (var i = 0; i < config.ConsumerCount; i++)
            {
                services.AddHostedService(sp => new KafkaConsumerHost(
                    config,
                    sp.GetRequiredKeyedService<InstrumentedConsumerBuilder<string, byte[]>>(config.ConsumerGroup.Id),
                    sp.GetRequiredService<IInboundPipeline>(),
                    sp.GetRequiredService<IDeadLetterHandler>(),
                    sp.GetRequiredService<ILogger<KafkaConsumerHost>>()));
            }

            return services;
        }
    }
}

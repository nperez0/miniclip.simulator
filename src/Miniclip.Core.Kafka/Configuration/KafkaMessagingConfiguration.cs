using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniclip.Core.Messaging;
using Miniclip.Core.Messaging.Pipeline.Configuration;

namespace Miniclip.Core.Kafka.Configuration;

public static class KafkaMessagingConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddKafkaMessagingInfrastructure(Action<PipelineOptions>? configurePipeline = null)
        {
            services.AddMessagingPipeline(configurePipeline);

            services.AddSingleton<IDeadLetterHandler, KafkaDeadLetterHandler>();

            return services;
        }

        public IServiceCollection AddKafkaConsumer(IKafkaConsumerConfig config)
        {
            services.AddHostedService(sp => new KafkaConsumerHost(
                config,
                sp.GetRequiredService<IMessagePipeline>(),
                sp.GetRequiredService<IDeadLetterHandler>(),
                sp.GetRequiredService<ILogger<KafkaConsumerHost>>()));

            return services;
        }
    }
}

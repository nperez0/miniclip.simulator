using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniclip.Core.Messaging.Outbound;
using Miniclip.Core.Messaging.Pipeline.Configuration;
using Miniclip.Core.Messaging.Pipeline.Inbound;
using Miniclip.Core.Messaging.Pipeline.Outbound;

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

        internal IServiceCollection AddOutboundKafkaInfrastructure(
            string bootstrapServers,
            Action<OutboundTopicMappingBuilder>? configureTopics = null)
        {
            var config = new ProducerConfig { BootstrapServers = bootstrapServers };

            services.AddOutboundPipeline();

            services.AddSingleton(new InstrumentedProducerBuilder<string, string>(config));
            services.AddSingleton<IProducer<string, string>>(sp =>
                sp.GetRequiredService<InstrumentedProducerBuilder<string, string>>().Build());

            var mappingBuilder = new OutboundTopicMappingBuilder();
            configureTopics?.Invoke(mappingBuilder);
            var topicMap = mappingBuilder.Build();

            services.AddSingleton<IOutboundTopicRegistry>(new OutboundTopicRegistry(topicMap));
            services.AddSingleton<IDestinationResolver, KafkaDestinationResolver>();
            services.AddScoped<IEventDispatcher, KafkaEventDispatcher>();
            services.AddScoped<IEventBus, OutboundPipeline>();

            return services;
        }

        internal IServiceCollection AddInboundKafkaInfrastructure(Action<PipelineOptions>? configurePipeline = null)
        {
            services.AddInboundPipeline(configurePipeline);
            services.AddMessageHandlers();

            services.AddSingleton<IDeadLetterHandler, KafkaDeadLetterHandler>();

            return services;
        }

        internal IServiceCollection AddKafkaConsumer(
            string bootstrapServers,
            Action<KafkaConsumerSubscriptionBuilder> configure)
        {
            var builder = new KafkaConsumerSubscriptionBuilder();
            configure(builder);
            var descriptor = builder.Build();

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = descriptor.Subscription.SubscriptionId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            // Registered as singleton so OTel can enumerate all descriptors
            // via IEnumerable<ConsumerSubscription> to wire consumer instrumentation.
            services.AddSingleton(descriptor.Subscription);

            // One InstrumentedConsumerBuilder per consumer group, keyed by subscription ID
            // so OTel resolves it via AddKafkaConsumerInstrumentation(subscriptionId).
            var consumerBuilder = new InstrumentedConsumerBuilder<string, string>(consumerConfig);
            services.AddKeyedSingleton(descriptor.Subscription.SubscriptionId, consumerBuilder);

            for (var i = 0; i < descriptor.Subscription.ConsumerCount; i++)
            {
                services.AddHostedService(sp =>
                {
                    var allHandlers = sp.GetServices<CompiledMessageHandler>();
                    var registry = BuildFilteredRegistry(
                        descriptor.Subscription,
                        allHandlers);

                    return new KafkaConsumerHost(
                        descriptor,
                        consumerBuilder,
                        sp.GetRequiredService<IInboundPipeline>(),
                        registry,
                        sp.GetRequiredService<IDeadLetterHandler>(),
                        sp.GetRequiredService<ILogger<KafkaConsumerHost>>());
                });
            }

            return services;
        }
    }

    private static IMessageHandlerRegistry BuildFilteredRegistry(
        ConsumerSubscription subscription,
        IEnumerable<CompiledMessageHandler> allHandlers)
    {
        var declared = subscription.MessageTypes.ToHashSet();

        var filtered = allHandlers
            .Where(h => declared.Contains(h.MessageType))
            .ToArray();

        return new MessageHandlerRegistry(filtered);
    }
}



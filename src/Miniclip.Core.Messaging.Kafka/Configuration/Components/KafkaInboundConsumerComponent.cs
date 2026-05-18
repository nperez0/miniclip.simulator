using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniclip.Core.Messaging.Configuration;
using Miniclip.Core.Messaging.Pipeline.Inbound;

namespace Miniclip.Core.Messaging.Kafka.Configuration.Components;

internal sealed class KafkaInboundConsumerComponent(
    string bootstrapServers,
    KafkaConsumerDescriptor descriptor) : IMessagingComponent
{
    public void Register(IServiceCollection services)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = descriptor.Subscription.SubscriptionId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        services.AddSingleton(descriptor.Subscription);

        var consumerBuilder = new InstrumentedConsumerBuilder<string, string>(consumerConfig);
        services.AddKeyedSingleton(descriptor.Subscription.SubscriptionId, consumerBuilder);

        for (var i = 0; i < descriptor.Subscription.ConsumerCount; i++)
        {
            services.AddHostedService(sp =>
            {
                var allHandlers = sp.GetServices<CompiledMessageHandler>();
                var registry = BuildFilteredRegistry(descriptor.Subscription, allHandlers);

                return new KafkaConsumerHost(
                    descriptor,
                    consumerBuilder,
                    sp.GetRequiredService<IInboundPipeline>(),
                    registry,
                    sp.GetRequiredService<IDeadLetterHandler>(),
                    sp.GetRequiredService<ILogger<KafkaConsumerHost>>());
            });
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

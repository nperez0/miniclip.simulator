using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Messaging.Pipeline.Configuration;

namespace Miniclip.Core.Messaging.Kafka.Configuration;

public sealed class InboundKafkaBuilder
{
    private IRetryPolicy retryPolicy = new ExponentialBackoffRetryPolicy(maxAttempts: 3);
    private readonly List<Action<KafkaConsumerSubscriptionBuilder>> consumers = [];

    public InboundKafkaBuilder UseRetryPolicy(IRetryPolicy policy)
    {
        retryPolicy = policy;
        return this;
    }

    public InboundKafkaBuilder AddConsumer(Action<KafkaConsumerSubscriptionBuilder> configure)
    {
        consumers.Add(configure);
        return this;
    }

    internal void Build(IServiceCollection services, string bootstrapServers)
    {
        services.AddInboundKafkaInfrastructure(options => options.RetryPolicy = retryPolicy);

        foreach (var configure in consumers)
            services.AddKafkaConsumer(bootstrapServers, configure);
    }
}
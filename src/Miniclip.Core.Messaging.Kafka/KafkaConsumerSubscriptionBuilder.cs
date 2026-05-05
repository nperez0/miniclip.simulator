using Miniclip.Core.Messaging.Pipeline.Inbound;

namespace Miniclip.Core.Messaging.Kafka;

public sealed class KafkaConsumerSubscriptionBuilder
{
    private readonly HandlerSubscriptionBuilder subscriptionBuilder = new();
    private readonly List<string> topics = [];

    public KafkaConsumerSubscriptionBuilder WithConsumerGroup(string id)
    {
        subscriptionBuilder.WithConsumerGroup(id);
        return this;
    }

    public KafkaConsumerSubscriptionBuilder Handles<TMessage>()
    {
        subscriptionBuilder.Handles<TMessage>();
        return this;
    }

    public KafkaConsumerSubscriptionBuilder WithConsumerCount(int count)
    {
        subscriptionBuilder.WithConsumerCount(count);
        return this;
    }

    public KafkaConsumerSubscriptionBuilder WithTopics(params string[] topicNames)
    {
        topics.AddRange(topicNames);
        return this;
    }

    internal KafkaConsumerDescriptor Build()
    {
        var subscription = subscriptionBuilder.Build();

        if (topics.Count == 0)
            throw new InvalidOperationException(
                "At least one topic must be declared via WithTopics(...) before building the Kafka consumer.");

        return new KafkaConsumerDescriptor(subscription, [.. topics]);
    }
}

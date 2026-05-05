namespace Miniclip.Core.Messaging.Pipeline.Inbound;

public sealed class HandlerSubscriptionBuilder
{
    private string? consumerGroupId;
    private readonly List<Type> messageTypes = [];
    private int consumerCount = 1;

    public HandlerSubscriptionBuilder WithConsumerGroup(string id)
    {
        consumerGroupId = id;
        return this;
    }

    public HandlerSubscriptionBuilder Handles<TMessage>()
    {
        messageTypes.Add(typeof(TMessage));
        return this;
    }

    public HandlerSubscriptionBuilder WithConsumerCount(int count)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Consumer count must be at least 1.");

        consumerCount = count;
        return this;
    }

    public ConsumerSubscription Build()
    {
        if (string.IsNullOrWhiteSpace(consumerGroupId))
            throw new InvalidOperationException(
                "A consumer group ID must be set via WithConsumerGroup(id) before building the subscription.");

        if (messageTypes.Count == 0)
            throw new InvalidOperationException(
                "At least one message type must be declared via Handles<TMessage>() before building the subscription.");

        return new ConsumerSubscription(consumerGroupId, [.. messageTypes], consumerCount);
    }
}
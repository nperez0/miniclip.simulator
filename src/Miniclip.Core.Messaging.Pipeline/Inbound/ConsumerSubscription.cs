namespace Miniclip.Core.Messaging.Pipeline.Inbound;

public sealed record ConsumerSubscription(
    string SubscriptionId,
    Type[] MessageTypes,
    int ConsumerCount);

using Miniclip.Core.Messaging.Pipeline.Inbound;

namespace Miniclip.Core.Messaging.Kafka;

public sealed record KafkaConsumerDescriptor(
    ConsumerSubscription Subscription,
    string[] Topics);

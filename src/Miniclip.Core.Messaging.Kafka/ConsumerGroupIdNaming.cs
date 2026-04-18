using Miniclip.Core.Domain;

namespace Miniclip.Core.Messaging.Kafka;

public static class ConsumerGroupIdNaming
{
    public static string ForAggregate<TAggregate>() where TAggregate : AggregateRoot
        => TopicNaming.ForAggregate<TAggregate>().Replace("simulator.", string.Empty);
}

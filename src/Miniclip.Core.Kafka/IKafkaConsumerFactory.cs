namespace Miniclip.Core.Kafka;

public interface IKafkaConsumerFactory
{
    IKafkaConsumer CreateConsumer(Func<KafkaMessageContext, CancellationToken, Task> onHandleAsync);
}
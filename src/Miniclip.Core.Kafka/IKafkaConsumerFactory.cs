namespace Miniclip.Core.Kafka;

public interface IKafkaConsumerFactory
{
    IKafkaConsumer CreateConsumer(
        IKafkaConsumerConfig config,
        Func<KafkaMessageContext, CancellationToken, Task> onHandleAsync);
}
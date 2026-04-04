namespace Miniclip.Core.Kafka;

public interface IKafkaConsumer
{
    Task ConsumeAsync(CancellationToken stoppingToken);
}

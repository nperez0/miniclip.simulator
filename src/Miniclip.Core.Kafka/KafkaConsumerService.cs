using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Miniclip.Core.Kafka;

public abstract class KafkaConsumerService(
    string[] topics,
    IConfiguration configuration,
    ILogger logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = configuration.GetConnectionString("kafka"),
            GroupId = ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        consumer.Subscribe(topics);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                await HandleAsync(result, stoppingToken);
                consumer.Commit(result);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { logger.LogError(ex, "Error processing Kafka message from {Topics}", topics); }
        }

        consumer.Close();
    }

    protected abstract string ConsumerGroupId { get; }

    protected abstract Task HandleAsync(
        ConsumeResult<string, byte[]> result,
        CancellationToken cancellationToken);
}

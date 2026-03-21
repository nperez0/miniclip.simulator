using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Miniclip.Core.Kafka.UnitTests.WhenExecutingConsumer;

public sealed class TestableKafkaConsumer(
    IConsumerRetryPolicy retryPolicy, 
    IConsumer<string, byte[]> consumer) 
    : KafkaConsumerService(["test-topic"], Substitute.For<IConfiguration>(), NullLogger.Instance, retryPolicy)
{
    public int HandleCallCount { get; private set; }
    public Exception? DeadLetterException { get; private set; }
    public Func<Task> HandleAction { get; set; } = () => Task.CompletedTask;

    public Task RunAsync(CancellationToken ct) => ExecuteAsync(ct);

    protected override string ConsumerGroupId => "test-group";

    protected override IConsumer<string, byte[]> BuildConsumer(ConsumerConfig config) => consumer;

    protected override Task OnDeadLetterAsync(
        ConsumeResult<string, byte[]> result,
        Exception exception,
        CancellationToken cancellationToken)
    {
        DeadLetterException = exception;
        return Task.CompletedTask;
    }

    protected override Task HandleAsync(ConsumeResult<string, byte[]> result, CancellationToken ct)
    {
        HandleCallCount++;
        return HandleAction();
    }
}

using Confluent.Kafka;
using Microsoft.Extensions.Logging.Abstractions;
using Miniclip.Core.Kafka.OpenTelemetry;
using NSubstitute;

namespace Miniclip.Core.Kafka.UnitTests.WhenExecutingConsumer;

public sealed class TestableKafkaConsumer(IConsumerRetryPolicy retryPolicy)
    : KafkaConsumerService(
        Substitute.For<IKafkaConsumerConfig>(),
        Substitute.For<IKafkaConsumerFactory>(),
        retryPolicy,
        Substitute.For<ITelemetryRecorderFactory>(),
        NullLogger.Instance)
{
    public int HandleCallCount { get; private set; }
    public Exception? DeadLetterException { get; private set; }
    public Func<Task> HandleAction { get; set; } = () => Task.CompletedTask;

    public Task InvokeHandleMessageAsync(KafkaMessageContext context, CancellationToken ct)
        => HandleMessageAsync(context, ct);

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

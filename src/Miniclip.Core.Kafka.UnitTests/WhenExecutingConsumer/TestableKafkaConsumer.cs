using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Miniclip.Core.Kafka.UnitTests.WhenExecutingConsumer;

public sealed class TestableKafkaConsumer(IConsumerRetryPolicy retryPolicy)
    : KafkaConsumerService(
        Substitute.For<IKafkaConsumerConfig>(),
        Substitute.For<IKafkaConsumerFactory>(),
        retryPolicy,
        NullLogger.Instance)
{
    public int HandleCallCount { get; private set; }
    public Exception? DeadLetterException { get; private set; }
    public Func<Task> HandleAction { get; set; } = () => Task.CompletedTask;

    public Task InvokeHandleMessageAsync(KafkaMessageContext context, CancellationToken ct)
        => HandleMessageAsync(context, ct);

    protected override Task OnDeadLetterAsync(
        KafkaMessageContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        DeadLetterException = exception;
        return Task.CompletedTask;
    }

    protected override Task HandleAsync(KafkaMessageContext context, CancellationToken ct)
    {
        HandleCallCount++;
        return HandleAction();
    }
}

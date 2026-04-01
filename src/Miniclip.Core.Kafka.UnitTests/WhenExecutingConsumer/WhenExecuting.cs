using Confluent.Kafka;
using Miniclip.Core.Tests;
using NSubstitute;

namespace Miniclip.Core.Kafka.UnitTests.WhenExecutingConsumer;

public abstract class WhenExecuting : AsyncTestBase<TestableKafkaConsumer>
{
    protected IConsumer<string, byte[]> MockConsumer { get; private set; } = null!;
    protected ConsumeResult<string, byte[]> TheResult { get; private set; } = null!;
    protected Func<Task> HandleAction { get; set; } = () => Task.CompletedTask;

    protected override Task GivenAsync()
    {
        MockConsumer = Substitute.For<IConsumer<string, byte[]>>();
        TheResult = new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]> { Key = "key", Value = [], Headers = [] }
        };

        var consumeCount = 0;
        MockConsumer.Consume(Arg.Any<CancellationToken>())
            .Returns(_ => consumeCount++ == 0 ? TheResult : throw new OperationCanceledException());

        return Task.CompletedTask;
    }

    protected override TestableKafkaConsumer CreateSystemUnderTest()
        => new(new ExponentialBackoffRetryPolicy(maxAttempts: 3, baseDelay: TimeSpan.Zero), MockConsumer)
        {
            HandleAction = HandleAction
        };

    protected override Task WhenAsync() => Sut!.RunAsync(CancellationToken.None);
}

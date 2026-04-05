using Confluent.Kafka;
using Miniclip.Core.Tests;
using NSubstitute;

namespace Miniclip.Core.Kafka.UnitTests.WhenExecutingConsumer;

public abstract class WhenExecuting : AsyncTestBase<TestableKafkaConsumer>
{
    protected ConsumeResult<string, byte[]> Result { get; private set; } = null!;
    protected KafkaMessageContext MessageContext { get; private set; } = null!;
    protected Func<Task> HandleAction { get; set; } = () => Task.CompletedTask;

    protected override Task GivenAsync()
    {
        var consumerConfig = Substitute.For<IKafkaConsumerConfig>();

        consumerConfig.ConsumerConfig.Returns(new ConsumerConfig { GroupId = "test-group" });

        Result = new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]> { Key = "key", Value = [], Headers = [] }
        };

        MessageContext = new KafkaMessageContext(Result);

        return Task.CompletedTask;
    }

    protected override TestableKafkaConsumer CreateSystemUnderTest()
        => new(new ExponentialBackoffRetryPolicy(maxAttempts: 3, baseDelay: TimeSpan.Zero))
        {
            HandleAction = HandleAction
        };

    protected override Task WhenAsync() => Sut!.InvokeHandleMessageAsync(MessageContext, CancellationToken.None);
}


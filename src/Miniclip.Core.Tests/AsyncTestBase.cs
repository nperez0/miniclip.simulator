using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NUnit.Framework;

namespace Miniclip.Core.Tests;

public abstract class AsyncTestBase<TSut>
    where TSut : class
{
    private bool recordException;

    protected TSut? Sut { get; private set; }

    protected IFixture Fixture { get; } = new Fixture().Customize(new AutoNSubstituteCustomization());

    protected Exception? ThrownException { get; private set; }

    [OneTimeSetUp]
    protected virtual async Task SetUp()
    {
        try
        {
            await GivenAsync().ConfigureAwait(false);

            Sut = CreateSystemUnderTest();

            await WhenAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (recordException)
                ThrownException = ex;
            else
                throw;
        }
    }

    protected virtual TSut CreateSystemUnderTest()
        => Fixture.Create<TSut>();

    protected virtual Task GivenAsync()
        => Task.CompletedTask;

    protected virtual Task WhenAsync()
        => Task.CompletedTask;

    protected void RecordAnyExceptionsThrown()
    {
        recordException = true;
    }
}

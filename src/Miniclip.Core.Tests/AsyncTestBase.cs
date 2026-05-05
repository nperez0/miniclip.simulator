using NUnit.Framework;

namespace Miniclip.Core.Tests;

public abstract class AsyncTestBase<TSut>
    where TSut : class
{
    private bool recordException;

    protected TSut? Sut { get; private set; }

    protected Exception? ThrownException { get; private set; }

    [SetUp]
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

    protected abstract TSut CreateSystemUnderTest();

    protected virtual async Task GivenAsync()
    {
        await SetupScenarioAsync().ConfigureAwait(false);
    }

    protected virtual Task SetupScenarioAsync()
        => Task.CompletedTask;

    protected virtual Task WhenAsync()
        => Task.CompletedTask;

    protected void RecordAnyExceptionsThrown()
    {
        recordException = true;
    }
}

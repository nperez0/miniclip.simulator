using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NUnit.Framework;

namespace Miniclip.Core.Tests;

public abstract class AsyncTestBase<TSut>
    where TSut : class
{
    private bool recordException;

    protected TSut? Sut { get; private set; }

    protected IFixture Fixture { get; private set; }

    protected Exception? ThrownException { get; private set; }

    protected AsyncTestBase()
    {
        recordException = false;

        Fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
    }

    [OneTimeSetUp]
    protected virtual async Task SetUp()
    {
        try
        {
            Given();

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

    protected virtual void Given() { }

    protected virtual ValueTask WhenAsync()
        => ValueTask.CompletedTask;

    protected void RecordAnyExceptionsThrown()
    {
        recordException = true;
    }
}

using NUnit.Framework;

namespace Miniclip.Core.Tests;

public abstract class TestBase<TSut>
    where TSut : class
{
    private bool recordException = false;

    protected TSut? Sut { get; private set; }

    protected Exception? ThrownException { get; private set; }

    [SetUp]
    protected virtual void SetUp()
    {
        Given();

        try
        {
            Sut = CreateSystemUnderTest();

            When();
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

    protected virtual void Given()
    {
        GivenScenario();
    }

    protected virtual void GivenScenario() { }

    protected virtual void When() { }

    protected void RecordAnyExceptionsThrown()
    {
        recordException = true;
    }
}

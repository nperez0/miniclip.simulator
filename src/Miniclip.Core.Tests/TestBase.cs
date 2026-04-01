using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NUnit.Framework;

namespace Miniclip.Core.Tests;

public abstract class TestBase<TSut>
    where TSut : class
{
    private bool recordException = false;

    protected TSut? Sut { get; private set; }

    protected IFixture Fixture { get; private set; } = new Fixture().Customize(new AutoNSubstituteCustomization());

    protected Exception? ThrownException { get; private set; }

    [OneTimeSetUp]
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

    protected virtual TSut CreateSystemUnderTest()
        => Fixture.Create<TSut>();

    protected virtual void Given() { }

    protected virtual void When() { }

    protected void RecordAnyExceptionsThrown()
    {
        recordException = true;
    }
}

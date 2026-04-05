using Microsoft.Extensions.Configuration;
using Miniclip.Core.Tests;
using Miniclip.Simulator.ReadModels.WebJob.Infrastructure.Configuration;

namespace Miniclip.Simulator.ReadModels.WebJob.UnitTests.Infrastructure.Configuration;

public abstract class WhenConfiguringHealthChecks : TestBase<ServiceProvider>
{
    protected IConfiguration Config { get; set; } = null!;

    protected override void Given()
    {
        Config = new ConfigurationBuilder().Build();
    }

    protected override ServiceProvider CreateSystemUnderTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Config);
        services.AddHealthChecksDependencies(Config);
        return services.BuildServiceProvider();
    }
}

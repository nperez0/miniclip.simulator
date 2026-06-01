using Microsoft.Extensions.Configuration;
using Miniclip.Core.ServiceDefaults.Configuration;
using Miniclip.Core.ServiceDefaults.HealthChecks;

namespace Miniclip.Simulator.ReadModels.WebJob.UnitTests.Infrastructure.Configuration;

public class WithPortConfigured : WhenConfiguringHealthChecks
{
    protected override void Given()
    {
        Config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [HealthCheckConfig.HealthCheckHttpPortListenerKey] = "8081"
            })
            .Build();
    }

    [Test]
    public void ShouldRegisterHttpListenerService()
    {
        Sut!.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .ShouldContain(s => s is HealthCheckHttpServerService);
    }
}


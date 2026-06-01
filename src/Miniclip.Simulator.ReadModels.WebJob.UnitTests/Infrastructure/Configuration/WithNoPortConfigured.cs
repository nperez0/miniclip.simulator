using Microsoft.Extensions.Diagnostics.HealthChecks;
using Miniclip.Core.ServiceDefaults.HealthChecks;

namespace Miniclip.Simulator.ReadModels.WebJob.UnitTests.Infrastructure.Configuration;

public class WithNoPortConfigured : WhenConfiguringHealthChecks
{
    [Test]
    public void ShouldRegisterHealthCheckService()
    {
        Sut!.GetService<HealthCheckService>().ShouldNotBeNull();
    }

    [Test]
    public void ShouldNotRegisterHttpListenerService()
    {
        Sut!.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .ShouldNotContain(s => s is HealthCheckHttpServerService);
    }
}

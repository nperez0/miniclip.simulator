using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Miniclip.Simulator.ReadModels.WebJob.Infrastructure;
using NUnit.Framework;

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

    [Test]
    public void ShouldRegisterMySqlHealthCheck()
    {
        var options = Sut!.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        options.Value.Registrations.ShouldContain(r => r.Name == "mysql");
    }
}

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Miniclip.Simulator.ReadModels.WebJob.UnitTests.Infrastructure.Configuration;

public class WhenRegistered : WhenConfiguringHealthChecks
{
    [Test]
    public void ShouldRegisterHealthCheckService()
        => Sut!.GetService<HealthCheckService>().ShouldNotBeNull();

    [Test]
    public void ShouldRegisterSelfHealthCheck()
    {
        var options = Sut!.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        options.Value.Registrations.ShouldContain(r => r.Name == "self");
    }

    [Test]
    public void ShouldRegisterMySqlHealthCheck()
    {
        var options = Sut!.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        options.Value.Registrations.ShouldContain(r => r.Name == "mysql");
    }
}
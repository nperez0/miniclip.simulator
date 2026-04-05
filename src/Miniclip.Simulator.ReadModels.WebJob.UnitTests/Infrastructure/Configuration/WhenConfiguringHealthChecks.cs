using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Miniclip.Simulator.ReadModels.WebJob.Infrastructure;
using Miniclip.Simulator.ReadModels.WebJob.Infrastructure.Configuration;

namespace Miniclip.Simulator.ReadModels.WebJob.UnitTests.Infrastructure.Configuration;

[TestFixture]
public class WhenConfiguringHealthChecks
{
    private static IServiceCollection BuildServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddHealthChecksDependencies(configuration);
        return services;
    }

    [Test]
    public void AlwaysRegisters_SelfLivenessCheck()
    {
        var configuration = new ConfigurationBuilder().Build();
        var provider = BuildServices(configuration).BuildServiceProvider();

        var healthCheckService = provider.GetService<HealthCheckService>();

        healthCheckService.ShouldNotBeNull();
    }

    [Test]
    public void WhenPortEnvVarSet_RegistersHostedService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [HealthCheckHttpServerService.HealthCheckHttpPortListenerKey] = "8081"
            })
            .Build();
        var provider = BuildServices(configuration).BuildServiceProvider();

        var hostedServices = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();

        hostedServices.ShouldContain(s => s is HealthCheckHttpServerService);
    }

    [Test]
    public void WhenPortEnvVarAbsent_DoesNotRegisterHostedService()
    {
        var configuration = new ConfigurationBuilder().Build();
        var provider = BuildServices(configuration).BuildServiceProvider();

        var hostedServices = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();

        hostedServices.ShouldNotContain(s => s is HealthCheckHttpServerService);
    }
}

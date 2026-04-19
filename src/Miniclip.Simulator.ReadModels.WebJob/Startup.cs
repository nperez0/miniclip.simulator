using Miniclip.Simulator.ReadModels.WebJob.Infrastructure.Configuration;

namespace Miniclip.Simulator.ReadModels.WebJob;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddReadModelsDbDependencies(configuration);
        services.AddProjectionsDependencies();
        services.AddKafkaDependencies(configuration);
        services.AddOpenTelemetryDependencies();
        services.AddHealthChecksDependencies(configuration);
    }

    public void Configure(IHost app)
    {
        app.InitializeDatabases();
    }
}

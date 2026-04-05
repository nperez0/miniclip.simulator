using Miniclip.Simulator.Api.Infrastructure.Configuration;
using Serilog;

namespace Miniclip.Simulator.Api;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddApiVersioningConfiguration();
        services.AddVersionedOpenApi();

        services.AddKafkaDependencies(configuration);
        services.AddMediatorDependencies();
        services.AddEventStoreDbDependencies(configuration);
        services.AddReadModelsDbDependencies(configuration);
        services.AddDomainDependencies();
        services.AddProjectionsDependencies();

        services.AddOpenTelemetryDependencies();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.InitializeDatabases();

        app.UseSerilogRequestLogging();

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapVersionedOpenApi();
        });
    }
}

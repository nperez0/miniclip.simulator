using Miniclip.Simulator.Api.Infrastructure.Configuration;

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
    }

    public void Configure(IApplicationBuilder app)
    {
        app.InitializeDatabases();

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

using Miniclip.Simulator.Api.Infrastructure.Configuration;

namespace Miniclip.Simulator.Api;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddApiVersioningConfiguration();
        services.AddEndpointsApiExplorer();
        services.AddVersionedOpenApi();

        services.AddMediatR();
        services.AddDatabaseDependencies(configuration);
        services.AddDomainDependencies();
        services.AddProjectionsDependencies();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.InitializeDatabases();

        app.UseVersionedOpenApi();
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

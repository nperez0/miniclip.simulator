using Miniclip.Core.Application.Behaviors;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class MediatRConfiguration
{
    public static IServiceCollection AddMediatR(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
         {
             cfg.RegisterServicesFromAssemblies(
                 typeof(Application.Commands.AssemblyReference).Assembly, // Commands
                 typeof(Application.Queries.AssemblyReference).Assembly, // Queries
                 typeof(ReadModels.Projections.AssemblyReference).Assembly); // Projections

             cfg.AddOpenBehavior(typeof(CommandUnitOfWorkBehavior<,>));
             cfg.AddOpenBehavior(typeof(ReadModelUnitOfWorkBehavior<,>));
             cfg.AddOpenBehavior(typeof(DomainEventPublisherBehavior<,>));
         });

        return services;
    }
}

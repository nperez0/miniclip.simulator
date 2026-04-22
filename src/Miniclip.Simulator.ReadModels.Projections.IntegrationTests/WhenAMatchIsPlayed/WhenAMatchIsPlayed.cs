using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.ReadModels.Projections;
using Miniclip.Core.ReadModels.Projections.Configuration;
using Miniclip.Simulator.Infrastructure.Read.Persistence;
using Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Write;
using Miniclip.Simulator.IntegrationEvents.V1;
using Miniclip.Simulator.ReadModels.Projections.Services;
using Write = Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.Projections.IntegrationTests.WhenAMatchIsPlayed;

public abstract class WhenAMatchIsPlayed
{
    protected ServiceProvider Services { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var sc = new ServiceCollection();

        var dbName = Guid.NewGuid().ToString();
        sc.AddDbContext<SimulatorReadDbContext>(o =>
            o.UseInMemoryDatabase(dbName));

        sc.AddScoped<Write.IGroupStandingsRepository, GroupStandingsRepository>();
        sc.AddScoped<Write.IMatchResultsRepository, MatchResultsRepository>();
        sc.AddScoped<IRecalculatePositionService, RecalculatePositionService>();
        sc.AddProjectionHandlers(typeof(MatchResultProjection).Assembly);

        Services = sc.BuildServiceProvider();

        await WhenAsync();
    }

    [OneTimeTearDown]
    public void TearDown() => Services.Dispose();

    protected virtual async Task WhenAsync()
    {
        foreach (var @event in Events)
        {
            using var scope = Services.CreateScope();
            var sp = scope.ServiceProvider;
            var dispatcher = sp.GetRequiredService<IProjectionDispatcher>();
            var context = sp.GetRequiredService<SimulatorReadDbContext>();

            await dispatcher.DispatchAsync(@event, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
        }
    }

    protected abstract IReadOnlyList<MatchPlayedIntegrationEvent> Events { get; }
}

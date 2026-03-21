using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Application.Behaviors;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
using Miniclip.Simulator.Infrastructure.Read.Persistence;
using Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Write;
using Miniclip.Simulator.ReadModels.Projections.Services;
using NUnit.Framework;
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

        sc.AddSingleton<INotificationPublisher, OrderedNotificationPublisher>();
        sc.AddMediator(o => o.ServiceLifetime = ServiceLifetime.Scoped);

        sc.AddScoped<Write.IGroupStandingsRepository, GroupStandingsRepository>();
        sc.AddScoped<Write.IMatchResultsRepository, MatchResultsRepository>();
        sc.AddScoped<IRecalculatePositionService, RecalculatePositionService>();

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
            var publisher = sp.GetRequiredService<IPublisher>();
            var context = sp.GetRequiredService<SimulatorReadDbContext>();

            await publisher.Publish(@event, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
        }
    }

    protected abstract IReadOnlyList<MatchPlayed> Events { get; }
}

using Miniclip.Core.Domain;
using Miniclip.Core.EventSourcing;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Api.Infrastructure.Seeding;

public class TeamDataSeeder(IServiceScopeFactory scopeFactory) : IHostedService
{
    private static readonly IReadOnlyList<(Guid Id, string Name, int Strength)> Teams =
    [
        (new Guid("aabc18b2-9029-43a7-82c2-58894987935b"), "Manchester City", 95),
        (new Guid("0f17d7ed-ca41-4d4e-ba6b-b19a71dcd7bd"), "Real Madrid", 93),
        (new Guid("c0e2a8a7-7a6a-4d70-96e4-7363f957a569"), "Bayern Munich", 92),
        (new Guid("e8cdc6b2-599f-40a8-9c5d-f3bfb856d81d"), "Paris Saint-Germain", 90),
        (new Guid("79095de2-dfef-4647-a984-cf2ffd9c917a"), "Liverpool", 88),
        (new Guid("f761f360-2d09-4019-8bb7-0420cb48f6ba"), "Barcelona", 85),
        (new Guid("a9af331c-f3ae-452e-b4f8-acd484957ff5"), "Juventus", 82),
        (new Guid("e07d01a1-2e29-466e-b036-7f192a019364"), "Chelsea", 80),
        (new Guid("4bf3078e-87ba-40c7-83c5-b3e29fd0453d"), "Atletico Madrid", 78),
        (new Guid("b50fed08-27e6-43d9-b516-69b3f1218b1a"), "Inter Milan", 75),
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository<Team>>();
        var session = scope.ServiceProvider.GetRequiredService<IEventStoreSession>();

        foreach (var (id, name, strength) in Teams)
        {
            if (await repository.FindAsync(id, cancellationToken) is not null)
                continue;

            var result = Team.Create(id, name, strength);
            if (result.IsSuccess)
                repository.Add(result.Value!);
        }

        await session.CommitAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

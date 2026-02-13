using Microsoft.EntityFrameworkCore;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence;

/// <summary>
/// DbContext for the read model (denormalized views).
/// Optimized for fast queries with pre-calculated data.
/// </summary>
public class SimulatorReadDbContext : DbContext
{
    public SimulatorReadDbContext(DbContextOptions<SimulatorReadDbContext> options)
        : base(options)
    {
    }

    public DbSet<GroupStandingsReadModel> GroupStandings => Set<GroupStandingsReadModel>();
    public DbSet<MatchResultReadModel> MatchResults => Set<MatchResultReadModel>();
    public DbSet<GroupOverviewReadModel> GroupOverviews => Set<GroupOverviewReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SimulatorReadDbContext).Assembly);
    }
}

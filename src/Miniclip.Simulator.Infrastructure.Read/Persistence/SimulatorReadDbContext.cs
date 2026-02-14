using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence;

public class SimulatorReadDbContext(DbContextOptions<SimulatorReadDbContext> options) : DbContext
{
    public DbSet<GroupStandingsReadModel> GroupStandings => Set<GroupStandingsReadModel>();
    public DbSet<MatchResultReadModel> MatchResults => Set<MatchResultReadModel>();
    public DbSet<GroupOverviewReadModel> GroupOverviews => Set<GroupOverviewReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SimulatorReadDbContext).Assembly);
    }

    public class AppContextDesignFactory : IDesignTimeDbContextFactory<SimulatorReadDbContext>
    {
        public SimulatorReadDbContext CreateDbContext(string[] args)
        {
            var connectionString = "Server=localhost;Port=4306;Database=MiniclipSimulator_Read;User=root;Password=root;";
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            var optionsBuilder = new DbContextOptionsBuilder<SimulatorReadDbContext>()
                .UseMySql(connectionString, serverVersion);

            return new SimulatorReadDbContext(optionsBuilder.Options);
        }
    }
}

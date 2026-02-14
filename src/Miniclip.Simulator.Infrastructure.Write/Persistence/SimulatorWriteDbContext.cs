using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Miniclip.Simulator.Infrastructure.Write.Persistence;

public class SimulatorWriteDbContext(DbContextOptions<SimulatorWriteDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SimulatorWriteDbContext).Assembly);
    }

    public class AppContextDesignFactory : IDesignTimeDbContextFactory<SimulatorWriteDbContext>
    {
        public SimulatorWriteDbContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                ?? "Server=localhost;Port=4306;Database=MiniclipSimulator_Write;User=root;Password=root;";
            
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            var optionsBuilder = new DbContextOptionsBuilder<SimulatorWriteDbContext>()
                .UseMySql(connectionString, serverVersion);

            return new SimulatorWriteDbContext(optionsBuilder.Options);
        }
    }
}

using Microsoft.EntityFrameworkCore;

namespace Miniclip.Simulator.Infrastructure.Write.Persistence;

public class SimulatorWriteDbContext(DbContextOptions<SimulatorWriteDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SimulatorWriteDbContext).Assembly);
    }
}

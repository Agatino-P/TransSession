using Shared.Database.Entities;
using Shared.Database.EntityConfigurations;

namespace Shared.Database;
public sealed class PocDbContext : DbContext
{
    public PocDbContext(DbContextOptions<PocDbContext> options) : base(options) { }

    public DbSet<PocLogEntry> LogEntries => Set<PocLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PocLogEntryConfiguration());
        
        base.OnModelCreating(modelBuilder);
    }
}

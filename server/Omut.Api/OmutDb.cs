using Microsoft.EntityFrameworkCore;
using Omut.Api.Models;

namespace Omut.Api;

// База данных (EF Core). В Docker это PostgreSQL, в автотестах база в памяти.
public class OmutDb(DbContextOptions<OmutDb> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Catch> Catches => Set<Catch>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Player>(e =>
        {
            e.HasIndex(p => p.Name).IsUnique();
            e.Property(p => p.Name).HasMaxLength(20);
        });

        model.Entity<Catch>(e =>
        {
            e.HasIndex(c => c.FishId);
            e.HasIndex(c => c.WeightKg);
            e.Property(c => c.FishId).HasMaxLength(40);
            e.Property(c => c.FishName).HasMaxLength(60);
            e.Property(c => c.Zone).HasMaxLength(20);
            e.HasOne(c => c.Player).WithMany(p => p.Catches).HasForeignKey(c => c.PlayerId);
        });
    }
}

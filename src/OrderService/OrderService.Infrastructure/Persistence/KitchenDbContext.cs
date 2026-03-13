using Microsoft.EntityFrameworkCore;

namespace OrderService.Infrastructure.Persistence;

public class KitchenDbContext : DbContext
{
    public KitchenDbContext(DbContextOptions<KitchenDbContext> options) : base(options)
    {
    }

    public DbSet<KitchenSlot> KitchenSlots => Set<KitchenSlot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("kitchen");
        
        modelBuilder.Entity<KitchenSlot>(entity =>
        {
            entity.HasKey(e => e.SlotStart);
        });
    }
}

public sealed class KitchenSlot
{
    public DateTime SlotStart { get; set; }
    public int Count { get; set; }
}
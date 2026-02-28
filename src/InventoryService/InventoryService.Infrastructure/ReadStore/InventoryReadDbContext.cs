using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.ReadStore;

public class InventoryReadDbContext : DbContext
{
    public InventoryReadDbContext(DbContextOptions<InventoryReadDbContext> options) : base(options)
    {
    }

    public DbSet<StockItemReadModel> StockItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StockItemReadModel>()
            .HasKey(x => x.ProductId);
    }
}

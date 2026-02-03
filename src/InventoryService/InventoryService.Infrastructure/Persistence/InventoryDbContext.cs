using InventoryService.Application.Models;
using InventoryService.Domain.Aggregates;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Hangfire;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }
    
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockReservation> StockReservation => Set<StockReservation>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedCommand> ProcessedCommands => Set<ProcessedCommand>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("inventory");
        modelBuilder.Entity<StockItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Ignore(e => e.DomainEvents);
            entity.HasIndex(e => e.ProductId).IsUnique();
            entity.Property(x => x.RowVersion)
                .IsRowVersion();
        });
        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.HasIndex(x => new { x.OrderId, x.ProductId })
                .IsUnique();
        });
    }
}
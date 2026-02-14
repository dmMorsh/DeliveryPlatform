using InventoryService.Application.Models;
using InventoryService.Domain.Aggregates;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Hangfire;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;

namespace InventoryService.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }
    
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockReservation> StockReservation => Set<StockReservation>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedCommand> ProcessedCommands => Set<ProcessedCommand>();// TODO переместить в HF бд
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("inventory");
        modelBuilder.Entity<StockItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Ignore(e => e.DomainEvents);
            entity.Property(x => x.RowVersion)
                .IsRowVersion();
        });
        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.HasIndex(x => new { x.OrderId, x.ProductId })
                .IsUnique();
        });

        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.Property(x => x.EventId)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(x => x.EventType)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(x => x.Topic)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(x => x.Status).IsRequired();
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;

namespace OrderService.Infrastructure.ReadStore;

public sealed class OrderReadDbContext : DbContext
{
    public OrderReadDbContext(DbContextOptions<OrderReadDbContext> options) : base(options)
    {
    }

    public DbSet<OrderReadModel> Orders => Set<OrderReadModel>();
    public DbSet<OrderReadItem> OrderItems => Set<OrderReadItem>();
    public DbSet<KitchenSlot> KitchenSlots => Set<KitchenSlot>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("order_read");

        modelBuilder.Entity<OrderReadModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ClientId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.KitchenSlotStart);
            entity.HasIndex(e => e.ExpectedReadyAt);
            entity.HasIndex(e => e.DeliveryZoneId);

            entity.Property(e => e.OrderNumber).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CourierNote).HasMaxLength(500);
            entity.Property(e => e.Currency).HasMaxLength(6);

            entity.HasMany(e => e.Items)
                .WithOne()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderReadItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
        });

        modelBuilder.Entity<KitchenSlot>(entity =>
        {
            entity.HasKey(e => e.SlotStart);
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

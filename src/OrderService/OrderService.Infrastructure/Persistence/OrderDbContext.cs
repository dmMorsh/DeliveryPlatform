using Microsoft.EntityFrameworkCore;
using OrderService.Application.Models;
using OrderService.Domain.Aggregates;

namespace OrderService.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Shared.Contracts.Events.ProcessedEvent> ProcessedEvents => Set<Shared.Contracts.Events.ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("order");
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.DomainEvents);
            entity.OwnsMany(e => e.Items);
            entity.OwnsOne(e => e.From);
            entity.OwnsOne(e => e.To);
            entity.OwnsOne(e => e.CostCents);
            
            entity.Property(e => e.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);            
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
            entity.Property(e => e.CourierNote)
                .HasMaxLength(500);

            entity.HasIndex(e => e.OrderNumber)
                .IsUnique();
            entity.HasIndex(e => e.ClientId);
            entity.HasIndex(e => e.CourierId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(x => x.RowVersion)
                .IsRowVersion();
        });

        modelBuilder.Entity<Shared.Contracts.Events.ProcessedEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.Property(x => x.EventId).IsRequired();
            entity.Property(x => x.EventType).IsRequired();
            entity.Property(x => x.Topic).IsRequired();
            entity.Property(x => x.Status).IsRequired();
        });
    }
}

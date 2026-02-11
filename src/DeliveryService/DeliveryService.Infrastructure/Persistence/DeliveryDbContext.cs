using DeliveryService.Application.Models;
using DeliveryService.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace DeliveryService.Infrastructure.Persistence;

public class DeliveryDbContext : DbContext
{
    public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : base(options)
    {
    }

    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Shared.Contracts.Events.ProcessedEvent> ProcessedEvents => Set<Shared.Contracts.Events.ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("delivery");

        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.DomainEvents);
            entity.Property(e => e.FromAddress).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ToAddress).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.OrderId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.Property(x => x.RowVersion)
                .IsRowVersion()
                .HasDefaultValue(Array.Empty<byte>());

            entity.OwnsMany(e => e.AssignmentAttempts, b =>
            {
                b.ToTable("DeliveryAssignmentAttempts", "delivery");
                b.WithOwner().HasForeignKey("DeliveryId");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.Property(x => x.CourierId).IsRequired();
                b.Property(x => x.Status).IsRequired();
                b.Property(x => x.OfferedAt).IsRequired();
            });
            entity.Navigation(e => e.AssignmentAttempts)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
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

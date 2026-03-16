using DeliveryService.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Contracts.Events;

namespace DeliveryService.Infrastructure.Persistence;

public class DeliveryDbContext : DbContext
{
    public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : base(options)
    {
    }

    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<ProcessedCommand> ProcessedCommands => Set<ProcessedCommand>();

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

        modelBuilder.Entity<ProcessedCommand>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CorrelationId, x.CommandType }).IsUnique();
            entity.Property(x => x.CommandType).IsRequired().HasMaxLength(255);
            entity.Property(x => x.ProcessedAt).IsRequired();
        });
    }
}
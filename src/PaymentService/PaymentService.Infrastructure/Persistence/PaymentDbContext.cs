using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Aggregates;
using Shared.Contracts;
using Shared.Contracts.Events;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("payment");
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Ignore(e => e.DomainEvents);
            entity.HasIndex(p => p.OrderId)
                .IsUnique();
            entity.Property(x => x.RowVersion)
                .IsRowVersion()
                .HasDefaultValue(Array.Empty<byte>());
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

using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Models;
using PaymentService.Domain.Aggregates;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Shared.Contracts.Events.ProcessedEvent> ProcessedEvents => Set<Shared.Contracts.Events.ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("payment");
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Ignore(e => e.DomainEvents);
            entity.HasIndex(p => p.OrderId)
                .IsUnique()
                .HasFilter($"\"{nameof(Payment.Status)}\" = {(int)PaymentStatus.Created}");
            entity.Property(x => x.RowVersion)
                .IsRowVersion()
                .HasDefaultValue(Array.Empty<byte>());
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

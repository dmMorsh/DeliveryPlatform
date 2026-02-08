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
                .IsRowVersion();
        });
    }
}
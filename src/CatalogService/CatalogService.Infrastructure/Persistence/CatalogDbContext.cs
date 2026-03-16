using CatalogService.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Contracts.Events;

namespace CatalogService.Infrastructure.Persistence;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Ignore(e => e.DomainEvents);
            entity.OwnsOne(e => e.PriceCents, price =>
            {
                price.Property(p => p.Currency)
                    .IsRequired()
                    .HasMaxLength(4);
            });
            entity.OwnsOne(e => e.WeightGrams);
            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(x => x.Description)
                .HasMaxLength(1000);
            entity.Property(x => x.RowVersion)
                .IsRowVersion();
        });

        modelBuilder.Entity<ProcessedEvent>(entity =>
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

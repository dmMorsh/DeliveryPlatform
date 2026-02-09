using CatalogService.Application.Models;
using CatalogService.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Shared.Contracts.Events.ProcessedEvent> ProcessedEvents => Set<Shared.Contracts.Events.ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Ignore(e => e.DomainEvents);
            entity.OwnsOne(e => e.PriceCents);
            entity.OwnsOne(e => e.WeightGrams);
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

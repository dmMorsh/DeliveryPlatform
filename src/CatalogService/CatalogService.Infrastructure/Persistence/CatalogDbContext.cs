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
    }
}
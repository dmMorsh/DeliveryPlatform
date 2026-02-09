using CartService.Application.Models;
using CartService.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace CartService.Infrastructure.Persistence;

public class CartDbContext : DbContext
{
    public CartDbContext(DbContextOptions<CartDbContext> options) : base(options)
    {
    }
    
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Shared.Contracts.Events.ProcessedEvent> ProcessedEvents => Set<Shared.Contracts.Events.ProcessedEvent>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("cart");
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Ignore(e => e.DomainEvents);
            entity.OwnsMany(x => x.Items);
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

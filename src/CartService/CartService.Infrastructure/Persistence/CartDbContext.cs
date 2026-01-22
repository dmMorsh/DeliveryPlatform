using CartService.Application.Models;
using CartService.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace CartService.Infrastructure.Persistence;

public class CartDbContext : DbContext
{
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Ignore(e => e.DomainEvents);
            entity.OwnsMany(x => x.Items);
        });
    }
}

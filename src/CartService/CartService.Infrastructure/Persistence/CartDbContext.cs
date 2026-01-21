using CartService.Application.Models;
using CartService.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace CartService.Infrastructure.Persistence;

public class CartDbContext : DbContext
{
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(
            "Host=localhost;Port=5432;Database=delivery_db;Username=postgres;Password=postgres");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(b =>
        {
            b.HasKey(x => x.Id);
            b.OwnsMany(x => x.Items);
        });
    }
}

using Microsoft.EntityFrameworkCore;
using OrderService.Application.Models;
using OrderService.Domain;

namespace OrderService.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.OwnsMany(e => e.Items);
            // Domain events are not part of the persistence model
            entity.Ignore(e => e.DomainEvents);
            
            entity.Property(e => e.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.From)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.To)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
            
            entity.Property(e => e.CourierNote)
                .HasMaxLength(500);

            entity.HasIndex(e => e.OrderNumber)
                .IsUnique();
            
            entity.HasIndex(e => e.ClientId);
            
            entity.HasIndex(e => e.CourierId);
            
            entity.HasIndex(e => e.Status);
            
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}

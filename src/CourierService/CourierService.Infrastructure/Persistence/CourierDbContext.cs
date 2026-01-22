using CourierService.Application.Models;
using CourierService.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

///application domain infrastucture interface
namespace CourierService.Infrastructure.Persistence;

public class CourierDbContext : DbContext
{
    public CourierDbContext(DbContextOptions<CourierDbContext> options) : base(options)
    {
    }

    public DbSet<Courier> Couriers => Set<Courier>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("couriers");
        modelBuilder.Entity<Courier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.DomainEvents);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DocumentNumber).IsRequired().HasMaxLength(50);
            
            entity.HasIndex(e => e.Phone).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.DocumentNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}

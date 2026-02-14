using Microsoft.EntityFrameworkCore;

namespace PaymentService.Infrastructure.Persistence;

public sealed class PaymentShardMapDbContext : DbContext
{
    public PaymentShardMapDbContext(DbContextOptions<PaymentShardMapDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentShardMap> PaymentShardMaps => Set<PaymentShardMap>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("payment");
        modelBuilder.Entity<PaymentShardMap>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ExternalPaymentId).IsUnique();
            entity.HasIndex(x => x.OrderId);
            entity.Property(x => x.ExternalPaymentId)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(x => x.Provider)
                .IsRequired()
                .HasMaxLength(50);
        });
    }
}

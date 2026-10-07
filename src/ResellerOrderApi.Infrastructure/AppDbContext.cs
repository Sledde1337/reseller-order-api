using Microsoft.EntityFrameworkCore;
using ResellerOrderApi.Core.Entities;

namespace ResellerOrderApi.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Reseller> Resellers => Set<Reseller>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.ProductId);
            e.Property(p => p.Ean).IsRequired().HasMaxLength(13);
            e.HasIndex(p => p.Ean).IsUnique();
            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
            e.Property(p => p.Category).HasConversion<string>().HasMaxLength(30);
            e.Property(p => p.NetPrice).HasPrecision(10, 2);
            e.Property(p => p.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Reseller>(e =>
        {
            e.HasKey(r => r.ResellerId);
            e.Property(r => r.Name).IsRequired().HasMaxLength(200);
            e.Property(r => r.Email).IsRequired().HasMaxLength(200);
            e.HasIndex(r => r.Email).IsUnique();
            e.Property(r => r.Country).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.OrderId);
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(o => o.TotalAmount).HasPrecision(12, 2);
            e.HasOne(o => o.Reseller)
                .WithMany(r => r.Orders)
                .HasForeignKey(o => o.ResellerId);
        });

        modelBuilder.Entity<OrderLine>(e =>
        {
            e.HasKey(l => l.OrderLineId);
            e.Property(l => l.UnitPrice).HasPrecision(10, 2);
            e.HasOne(l => l.Order)
                .WithMany(o => o.Lines)
                .HasForeignKey(l => l.OrderId);
            e.HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId);
        });

        modelBuilder.Entity<StockMovement>(e =>
        {
            e.HasKey(s => s.StockMovementId);
            e.Property(s => s.Reason).HasConversion<string>().HasMaxLength(30);
            e.HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductId);
        });
    }
}
using DemoForMuhin.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoForMuhin.Data;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSize> ProductSizes => Set<ProductSize>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products", table => table.HasCheckConstraint("ck_product_price", "\"Price\" >= 0"));
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Manufacturer).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Material).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Price).HasPrecision(12, 2);
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ProductSize>(entity =>
        {
            entity.ToTable("product_sizes", table => table.HasCheckConstraint("ck_available_quantity", "\"AvailableQuantity\" >= 0"));
            entity.HasIndex(x => new { x.ProductId, x.Size }).IsUnique();
            entity.HasOne(x => x.Product).WithMany(x => x.Sizes).HasForeignKey(x => x.ProductId);
        });
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OrderedOn).HasColumnType("date");
        });
        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.ToTable("order_lines", table => table.HasCheckConstraint("ck_order_quantity", "\"Quantity\" > 0"));
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.HasOne(x => x.Order).WithMany(x => x.Lines).HasForeignKey(x => x.OrderId);
            entity.HasOne(x => x.Product).WithMany(x => x.OrderLines).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

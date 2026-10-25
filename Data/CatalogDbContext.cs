using DemoForMuhin.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoForMuhin.Data;

public class CatalogDbContext : DbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ShoeSize> Sizes => Set<ShoeSize>();
    public DbSet<ProductSize> ProductSizes => Set<ProductSize>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql(DatabaseSettings.GetConnectionString());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.Entity<Category>().ToTable("categories");
        modelBuilder.Entity<Category>().Property(x => x.Id).HasColumnName("id");
        modelBuilder.Entity<Category>().Property(x => x.Name).HasColumnName("name");
        modelBuilder.Entity<ShoeSize>().ToTable("sizes");
        modelBuilder.Entity<ShoeSize>().Property(x => x.Id).HasColumnName("id");
        modelBuilder.Entity<ShoeSize>().Property(x => x.Value).HasColumnName("value").HasPrecision(4, 1);

        var product = modelBuilder.Entity<Product>();
        product.ToTable("products");
        product.Property(x => x.Id).HasColumnName("id");
        product.Property(x => x.CategoryId).HasColumnName("category_id");
        product.Property(x => x.Subcategory).HasColumnName("subcategory");
        product.Property(x => x.Name).HasColumnName("name");
        product.Property(x => x.Manufacturer).HasColumnName("manufacturer");
        product.Property(x => x.Material).HasColumnName("material");
        product.Property(x => x.Description).HasColumnName("description");
        product.Property(x => x.ImageFile).HasColumnName("image_file");
        product.Property(x => x.Price).HasColumnName("price").HasPrecision(12, 2);
        product.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId);

        var stock = modelBuilder.Entity<ProductSize>();
        stock.ToTable("product_sizes");
        stock.Property(x => x.Id).HasColumnName("id");
        stock.Property(x => x.ProductId).HasColumnName("product_id");
        stock.Property(x => x.SizeId).HasColumnName("size_id");
        stock.Property(x => x.AvailableQuantity).HasColumnName("available_quantity");
        stock.HasOne(x => x.Product).WithMany(x => x.Sizes).HasForeignKey(x => x.ProductId);
        stock.HasOne(x => x.Size).WithMany().HasForeignKey(x => x.SizeId);
    }
}

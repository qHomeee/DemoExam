using DemoForMuhin.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoForMuhin.Data;

public class CatalogDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ShoeSize> Sizes => Set<ShoeSize>();
    public DbSet<ProductSize> ProductSizes => Set<ProductSize>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql(DatabaseSettings.GetConnectionString());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");
        var role = modelBuilder.Entity<Role>();
        role.ToTable("roles");
        role.Property(x => x.Id).HasColumnName("id");
        role.Property(x => x.Name).HasColumnName("name").HasMaxLength(80);
        role.HasIndex(x => x.Name).IsUnique();

        var user = modelBuilder.Entity<User>();
        user.ToTable("users");
        user.Property(x => x.Id).HasColumnName("id");
        user.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(80);
        user.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(80);
        user.Property(x => x.Patronymic).HasColumnName("patronymic").HasMaxLength(80);
        user.Property(x => x.Login).HasColumnName("login").HasMaxLength(80);
        user.Property(x => x.RoleId).HasColumnName("role_id");
        user.HasIndex(x => x.Login).IsUnique();
        user.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);

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

        var order = modelBuilder.Entity<Order>();
        order.ToTable("orders");
        order.Property(x => x.Id).HasColumnName("id");
        order.Property(x => x.OrderDate).HasColumnName("order_date");
        order.Property(x => x.CustomerId).HasColumnName("customer_id");
        order.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);

        var item = modelBuilder.Entity<OrderItem>();
        item.ToTable("order_items");
        item.HasKey(x => new { x.OrderId, x.ProductSizeId });
        item.Property(x => x.OrderId).HasColumnName("order_id");
        item.Property(x => x.ProductSizeId).HasColumnName("product_size_id");
        item.Property(x => x.Quantity).HasColumnName("quantity");
        item.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2);
        item.HasOne<Order>().WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        item.HasOne(x => x.ProductSize).WithMany().HasForeignKey(x => x.ProductSizeId).OnDelete(DeleteBehavior.Restrict);
    }
}

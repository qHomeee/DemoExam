using System;
using System.Threading.Tasks;
using DemoForMuhin.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoForMuhin.Data;

public static class DemoData
{
    // Только явная команда --seed-demo создаёт схему и учебные записи.
    // Обычный запуск приложения выполняет только чтение.
    public static async Task SeedAsync(CatalogDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.Products.AnyAsync())
            return;

        var sport = new Category { Name = "Кроссовки" };
        var casual = new Category { Name = "Повседневная обувь" };
        var boots = new Category { Name = "Ботинки" };
        var products = new[]
        {
            Create("Кроссовки Urban Step", "Кросс", "Текстиль, резина", "Лёгкая модель для прогулок по городу.", 4290m, sport, "runner.png", (39, 4), (40, 5), (41, 3)),
            Create("Кеды Canvas Low", "Линия", "Хлопок, резина", "Низкие кеды с мягкой стелькой.", 3190m, casual, "canvas.png", (38, 1), (39, 2)),
            Create("Ботинки North Trail", "Север", "Натуральная кожа, ТЭП", "Демисезонные ботинки с рельефной подошвой.", 7490m, boots, "boot.png", (41, 4), (42, 2), (43, 2)),
            Create("Лоферы Classic Line", "Линия", "Натуральная кожа", "Повседневная модель без шнуровки.", 5590m, casual, "loafer.png", (40, 2), (41, 2)),
            Create("Кроссовки Sprint Air", "Кросс", "Сетка, искусственная кожа", "Дышащий верх и гибкая подошва.", 6190m, sport, "sprint.png", (40, 3), (41, 4), (42, 3)),
            Create("Ботинки Desert Sand", "Север", "Замша, резина", "Лаконичные ботинки из мягкой замши.", 6790m, boots, null, (42, 0), (43, 0))
        };
        db.Products.AddRange(products);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        // У части моделей были заказы в прошлом календарном месяце: скидка им не положена.
        db.Orders.Add(new Order
        {
            OrderedOn = monthStart.AddMonths(-1).AddDays(4),
            CustomerName = "Учебный покупатель",
            Lines =
            {
                new OrderLine { Product = products[0], Size = 40, Quantity = 1, UnitPrice = products[0].Price },
                new OrderLine { Product = products[2], Size = 42, Quantity = 1, UnitPrice = products[2].Price },
                new OrderLine { Product = products[4], Size = 41, Quantity = 1, UnitPrice = products[4].Price }
            }
        });
        await db.SaveChangesAsync();
    }

    private static Product Create(string name, string manufacturer, string material, string description,
        decimal price, Category category, string? image, params (int Size, int Quantity)[] stock)
    {
        var product = new Product { Name = name, Manufacturer = manufacturer, Material = material,
            Description = description, Price = price, Category = category, ImageFile = image };
        foreach (var (size, quantity) in stock)
            product.Sizes.Add(new ProductSize { Size = size, AvailableQuantity = quantity });
        return product;
    }
}

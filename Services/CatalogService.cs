using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoForMuhin.Data;
using Microsoft.EntityFrameworkCore;

namespace DemoForMuhin.Services;

public record CatalogItem(int Id, string Name, string Category, string Manufacturer, string Material,
    string Description, string? ImageFile, decimal OriginalPrice, decimal CurrentPrice,
    bool HasDiscount, int AvailableQuantity);

public class CatalogService
{
    public async Task<IReadOnlyList<CatalogItem>> LoadAsync(DateOnly today)
    {
        await using var db = new CatalogDbContext(DatabaseSettings.CreateOptions());
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var previousMonthStart = monthStart.AddMonths(-1);
        return await db.Products.AsNoTracking().OrderBy(x => x.Id)
            .Select(product => new CatalogItem(
                product.Id, product.Name, product.Category.Name, product.Manufacturer,
                product.Material, product.Description, product.ImageFile, product.Price,
                product.OrderLines.Any(line => line.Order.OrderedOn >= previousMonthStart && line.Order.OrderedOn < monthStart)
                    ? product.Price : product.Price * 0.75m,
                !product.OrderLines.Any(line => line.Order.OrderedOn >= previousMonthStart && line.Order.OrderedOn < monthStart),
                product.Sizes.Sum(size => size.AvailableQuantity)))
            .ToListAsync();
    }
}


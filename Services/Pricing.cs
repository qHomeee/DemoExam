using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoForMuhin.Data;
using Microsoft.EntityFrameworkCore;

namespace DemoForMuhin.Services;

public static class Pricing
{
    public static decimal SalePrice(decimal price, bool hasDiscount) => hasDiscount
        ? Math.Round(price * 0.75m, 2, MidpointRounding.AwayFromZero)
        : price;

    public static async Task<HashSet<int>> ProductsOrderedLastMonthAsync(CatalogDbContext db, DateOnly today)
    {
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var previousMonthStart = monthStart.AddMonths(-1);
        var productIds = await db.Orders
            .Where(order => order.OrderDate >= previousMonthStart && order.OrderDate < monthStart)
            .SelectMany(order => order.Items)
            .Select(item => item.ProductSize.ProductId)
            .Distinct()
            .ToListAsync();
        return productIds.ToHashSet();
    }
}
